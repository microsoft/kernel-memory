﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Azure;
using Azure.Identity;
using Azure.Storage;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticMemory.Diagnostics;
using Timer = System.Timers.Timer;

namespace Microsoft.SemanticMemory.Pipeline.Queue.AzureQueues;

public sealed class AzureQueue : IQueue
{
    private const string DefaultEndpointSuffix = "core.windows.net";

    private sealed class MessageEventArgs : EventArgs
    {
        public QueueMessage? Message { get; set; }
    }

    /// <summary>
    /// Event triggered when a message is received
    /// </summary>
    private event AsyncMessageHandler<MessageEventArgs>? Received;

    // How often to check if there are new messages
    private const int PollDelayMsecs = 100;

    // How many messages to fetch at a time
    private const int FetchBatchSize = 3;

    // How long to lock messages once fetched. Azure Queue default is 30 secs.
    private const int FetchLockSeconds = 300;

    // How many times to dequeue a messages and process before moving it to a poison queue
    private const int MaxRetryBeforePoisonQueue = 20;

    // Suffix used for the poison queues
    private const string PoisonQueueSuffix = "-poison";

    // Queue client builder, requiring the queue name in input
    private readonly Func<string, QueueClient> _clientBuilder;

    // Queue client, once connected
    private QueueClient? _queue;

    // Queue client, once connected
    private QueueClient? _poisonQueue;

    // Name of the queue
    private string _queueName = string.Empty;

    // Timer triggering the message dispatch
    private Timer? _dispatchTimer;

    // Application logger
    private readonly ILogger<AzureQueue> _log;

    // Lock helpers
    private readonly object _lock = new();
    private bool _busy = false;
    private readonly CancellationTokenSource _cancellation = new();

    public AzureQueue(
        AzureQueueConfig config,
        ILogger<AzureQueue>? log = null)
    {
        this._log = log ?? DefaultLogger<AzureQueue>.Instance;

        switch (config.Auth)
        {
            case AzureQueueConfig.AuthTypes.ConnectionString:
            {
                this.ValidateConnectionString(config.ConnectionString);
                this._clientBuilder = queueName => new QueueClient(config.ConnectionString, queueName);
                break;
            }

            case AzureQueueConfig.AuthTypes.AccountKey:
            {
                this.ValidateAccountName(config.Account);
                this.ValidateAccountKey(config.AccountKey);
                var suffix = this.ValidateEndpointSuffix(config.EndpointSuffix);
                this._clientBuilder = queueName => new QueueClient(new($"https://{config.Account}.queue.{suffix}/{queueName}"), new StorageSharedKeyCredential(config.Account, config.AccountKey));
                break;
            }

            case AzureQueueConfig.AuthTypes.AzureIdentity:
            {
                this.ValidateAccountName(config.Account);
                var suffix = this.ValidateEndpointSuffix(config.EndpointSuffix);
                this._clientBuilder = queueName => new QueueClient(new($"https://{config.Account}.queue.{suffix}/{queueName}"), new DefaultAzureCredential());
                break;
            }

            case AzureQueueConfig.AuthTypes.ManualStorageSharedKeyCredential:
            {
                this.ValidateAccountName(config.Account);
                var suffix = this.ValidateEndpointSuffix(config.EndpointSuffix);
                this._clientBuilder = queueName => new QueueClient(new($"https://{config.Account}.queue.{suffix}/{queueName}"), config.GetStorageSharedKeyCredential());
                break;
            }

            case AzureQueueConfig.AuthTypes.ManualAzureSasCredential:
            {
                this.ValidateAccountName(config.Account);
                var suffix = this.ValidateEndpointSuffix(config.EndpointSuffix);
                this._clientBuilder = queueName => new QueueClient(new($"https://{config.Account}.queue.{suffix}/{queueName}"), config.GetAzureSasCredential());
                break;
            }

            case AzureQueueConfig.AuthTypes.ManualTokenCredential:
            {
                this.ValidateAccountName(config.Account);
                var suffix = this.ValidateEndpointSuffix(config.EndpointSuffix);
                this._clientBuilder = queueName => new QueueClient(new($"https://{config.Account}.queue.{suffix}/{queueName}"), config.GetTokenCredential());
                break;
            }

            default:
                this._log.LogCritical("Azure Queue authentication type '{0}' undefined or not supported", config.Auth);
                throw new ContentStorageException($"Azure Queue authentication type '{config.Auth}' undefined or not supported");
        }
    }

    /// <inherit />
    public async Task<IQueue> ConnectToQueueAsync(string queueName, QueueOptions options = default, CancellationToken cancellationToken = default)
    {
        queueName = CleanQueueName(queueName);
        this._log.LogTrace("Connecting to queue name: {0}", queueName);

        if (string.IsNullOrEmpty(queueName))
        {
            this._log.LogError("The queue name is empty");
            throw new ArgumentOutOfRangeException(nameof(queueName), "The queue name is empty");
        }

        if (!string.IsNullOrEmpty(this._queueName))
        {
            this._log.LogError("The queue name has already been set");
            throw new InvalidOperationException($"The queue is already connected to `{this._queueName}`");
        }

        // Note: 3..63 chars, only lowercase letters, numbers and hyphens. No hyphens at start/end and no consecutive hyphens.
        this._queueName = queueName;
        this._log.LogDebug("Queue name: {0}", this._queueName);

        this._queue = this._clientBuilder(this._queueName);
        Response? result = await this._queue.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        this._log.LogTrace("Queue ready: status code {0}", result?.Status);

        this._poisonQueue = this._clientBuilder(this._queueName + PoisonQueueSuffix);
        result = await this._poisonQueue.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        this._log.LogTrace("Poison queue ready: status code {0}", result?.Status);

        if (options.DequeueEnabled)
        {
            this._log.LogTrace("Enabling dequeue on queue {0}, every {1} msecs", this._queueName, PollDelayMsecs);
            this._dispatchTimer = new Timer(PollDelayMsecs); // milliseconds
            this._dispatchTimer.Elapsed += this.DispatchMessages;
            this._dispatchTimer.Start();
        }

        return this;
    }

    /// <inherit />
    public async Task EnqueueAsync(string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(this._queueName) || this._queue == null)
        {
            this._log.LogError("The queue client is not connected, cannot enqueue messages");
            throw new InvalidOperationException("The client must be connected to a queue first");
        }

        this._log.LogDebug("Sending message...");
        Response<SendReceipt> receipt = await this._queue.SendMessageAsync(message, cancellationToken).ConfigureAwait(false);
        this._log.LogDebug("Message sent {0}", receipt.Value?.MessageId);
    }

    /// <inherit />
    public void OnDequeue(Func<string, Task<bool>> processMessageAction)
    {
        this.Received += async (object sender, MessageEventArgs args) =>
        {
            QueueMessage message = args.Message!;

            this._log.LogInformation("Message '{0}' received, expires at {1}", message.MessageId, message.ExpiresOn);

            try
            {
                if (message.DequeueCount <= MaxRetryBeforePoisonQueue)
                {
                    bool success = await processMessageAction.Invoke(message.MessageText).ConfigureAwait(false);
                    if (success)
                    {
                        this._log.LogDebug("Message '{0}' dispatch successful, deleting message", message.MessageId);
                        await this.DeleteMessageAsync(message).ConfigureAwait(false);
                    }
                    else
                    {
                        var backoffDelay = TimeSpan.FromSeconds(1 * message.DequeueCount);
                        this._log.LogWarning("Message '{0}' dispatch rejected, putting message back in the queue with a delay of {1} msecs",
                            message.MessageId, backoffDelay.TotalMilliseconds);
                        await this.UnlockMessageAsync(message, backoffDelay).ConfigureAwait(false);
                    }
                }
                else
                {
                    this._log.LogError("Message '{0}' reached max attempts, moving to poison queue", message.MessageId);
                    await this.MoveMessageToPoisonQueueAsync(message).ConfigureAwait(false);
                }
            }
#pragma warning disable CA1031 // Must catch all to handle queue properly
            catch (Exception e)
            {
                // Exceptions caught by this block:
                // - message processing failed with exception
                // - failed to delete message from queue
                // - failed to unlock message in the queue
                // - failed to move message to poison queue

                var backoffDelay = TimeSpan.FromSeconds(1 * message.DequeueCount);
                this._log.LogWarning(e, "Message '{0}' processing failed with exception, putting message back in the queue with a delay of {1} msecs",
                    message.MessageId, backoffDelay.TotalMilliseconds);

                // Note: if this fails, the exception is caught by this.DispatchMessages()
                await this.UnlockMessageAsync(message, backoffDelay).ConfigureAwait(false);
            }
#pragma warning restore CA1031
        };
    }

    /// <inherit />
    public void Dispose()
    {
        this._cancellation.Cancel();
        this._cancellation.Dispose();
        this._dispatchTimer?.Dispose();
    }

    /// <summary>
    /// Fetch messages from the queue and dispatch them
    /// </summary>
    private void DispatchMessages(object? sender, ElapsedEventArgs ev)
    {
        if (this._busy || this.Received == null || this._queue == null)
        {
            return;
        }

        lock (this._lock)
        {
            this._busy = true;

            QueueMessage[] messages = Array.Empty<QueueMessage>();

            // Fetch messages
            try
            {
                // Fetch and Hide N messages
                Response<QueueMessage[]> receiveMessages = this._queue.ReceiveMessages(FetchBatchSize, visibilityTimeout: TimeSpan.FromSeconds(FetchLockSeconds));
                if (receiveMessages.HasValue && receiveMessages.Value.Length > 0)
                {
                    messages = receiveMessages.Value;
                }
            }
            catch (Exception exception)
            {
                this._log.LogError(exception, "Fetch failed");
                this._busy = false;
                throw;
            }

            if (messages.Length == 0)
            {
                this._busy = false;
                return;
            }

            // Async messages dispatch
            this._log.LogTrace("Dispatching {0} messages", messages.Length);
            foreach (QueueMessage message in messages)
            {
                _ = Task.Factory.StartNew(
                    function: async _ =>
                    {
                        try
                        {
                            this._log.LogTrace("Message content: {0}", message.MessageText);
                            await this.Received(this, new MessageEventArgs { Message = message }).ConfigureAwait(false);
                        }
#pragma warning disable CA1031 // Must catch all to log and keep the process alive
                        catch (Exception e)
                        {
                            this._log.LogError(e, "Message '{0}' processing failed with exception", message.MessageId);
                        }
#pragma warning restore CA1031
                    },
                    state: null,
                    cancellationToken: this._cancellation.Token,
                    creationOptions: TaskCreationOptions.RunContinuationsAsynchronously,
                    scheduler: TaskScheduler.Current
                );
            }

            this._busy = false;
        }
    }

    private Task DeleteMessageAsync(QueueMessage message)
    {
        return this._queue!.DeleteMessageAsync(message.MessageId, message.PopReceipt);
    }

    private Task UnlockMessageAsync(QueueMessage message, TimeSpan delay)
    {
        return this._queue!.UpdateMessageAsync(message.MessageId, message.PopReceipt, visibilityTimeout: delay);
    }

    private async Task MoveMessageToPoisonQueueAsync(QueueMessage message)
    {
        await this._poisonQueue!.CreateIfNotExistsAsync().ConfigureAwait(false);

        var poisonMsg = new
        {
            MessageText = message.MessageText,
            Id = message.MessageId,
            InsertedOn = message.InsertedOn,
            DequeueCount = message.DequeueCount,
        };

        var neverExpire = TimeSpan.FromSeconds(-1);
        await this._poisonQueue.SendMessageAsync(
            ToJson(poisonMsg),
            visibilityTimeout: TimeSpan.Zero,
            timeToLive: neverExpire).ConfigureAwait(false);
        await this.DeleteMessageAsync(message).ConfigureAwait(false);
    }

    private void ValidateAccountName(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            this._log.LogCritical("The Azure Queue account name is empty");
            throw new ContentStorageException("The account name is empty");
        }
    }

    private void ValidateAccountKey(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            this._log.LogCritical("The Azure Queue account key is empty");
            throw new ContentStorageException("The Azure Queue account key is empty");
        }
    }

    private void ValidateConnectionString(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            this._log.LogCritical("The Azure Queue connection string is empty");
            throw new ContentStorageException("The Azure Queue connection string is empty");
        }
    }

    private string ValidateEndpointSuffix(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            value = DefaultEndpointSuffix;
            this._log.LogError("The Azure Queue account endpoint suffix is empty, using default value {0}", value);
        }

        return value;
    }

    private static string ToJson(object data, bool indented = false)
    {
        return JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = indented });
    }

    private static string CleanQueueName(string? name)
    {
        return name?.ToLowerInvariant().Replace('_', '-').Replace(' ', '-').Replace('.', '-') ?? string.Empty;
    }
}
