// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Diagnostics.CodeAnalysis;
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
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.DocumentStorage;
using Microsoft.KernelMemory.Pipeline.Queue;
using Timer = System.Timers.Timer;

namespace Microsoft.KernelMemory.Orchestration.AzureQueues;

[Experimental("KMEXP04")]
public sealed class AzureQueuesPipeline : IQueue
{
    private const string DefaultEndpointSuffix = "core.windows.net";

    private static readonly JsonSerializerOptions s_indentedJsonOptions = new() { WriteIndented = true };
    private static readonly JsonSerializerOptions s_notIndentedJsonOptions = new() { WriteIndented = false };

    private sealed class MessageEventArgs : EventArgs
    {
        public QueueMessage? Message { get; set; }
    }

    /// <summary>
    /// Event triggered when a message is received
    /// </summary>
    private event AsyncMessageHandler<MessageEventArgs>? Received;

    // Queue client builder, requiring the queue name in input
    private readonly Func<string, QueueClient> _clientBuilder;

    // Queue confirguration
    private readonly AzureQueuesConfig _config;

    // Queue client, once connected
    private QueueClient? _queue;

    // Queue client, once connected
    private QueueClient? _poisonQueue;

    // Name of the queue
    private string _queueName = string.Empty;

    // Timer triggering the message dispatch
    private Timer? _dispatchTimer;

    // Application logger
    private readonly ILogger<AzureQueuesPipeline> _log;

    // Lock helpers
    private readonly object _lock = new();
    private bool _busy = false;
    private readonly CancellationTokenSource _cancellation = new();

    public AzureQueuesPipeline(
        AzureQueuesConfig config,
        ILoggerFactory? loggerFactory = null)
    {
        this._config = config;
        this._config.Validate();

        this._log = (loggerFactory ?? DefaultLogger.Factory).CreateLogger<AzureQueuesPipeline>();

        switch (config.Auth)
        {
            case AzureQueuesConfig.AuthTypes.ConnectionString:
            {
                this.ValidateConnectionString(config.ConnectionString);
                this._clientBuilder = queueName => new QueueClient(config.ConnectionString, queueName);
                break;
            }

            case AzureQueuesConfig.AuthTypes.AccountKey:
            {
                this.ValidateAccountName(config.Account);
                this.ValidateAccountKey(config.AccountKey);
                var suffix = this.ValidateEndpointSuffix(config.EndpointSuffix);
                this._clientBuilder = queueName => new QueueClient(new($"https://{config.Account}.queue.{suffix}/{queueName}"), new StorageSharedKeyCredential(config.Account, config.AccountKey));
                break;
            }

            case AzureQueuesConfig.AuthTypes.AzureIdentity:
            {
                this.ValidateAccountName(config.Account);
                var suffix = this.ValidateEndpointSuffix(config.EndpointSuffix);
                this._clientBuilder = queueName => new QueueClient(new($"https://{config.Account}.queue.{suffix}/{queueName}"), new DefaultAzureCredential());
                break;
            }

            case AzureQueuesConfig.AuthTypes.ManualStorageSharedKeyCredential:
            {
                this.ValidateAccountName(config.Account);
                var suffix = this.ValidateEndpointSuffix(config.EndpointSuffix);
                this._clientBuilder = queueName => new QueueClient(new($"https://{config.Account}.queue.{suffix}/{queueName}"), config.GetStorageSharedKeyCredential());
                break;
            }

            case AzureQueuesConfig.AuthTypes.ManualAzureSasCredential:
            {
                this.ValidateAccountName(config.Account);
                var suffix = this.ValidateEndpointSuffix(config.EndpointSuffix);
                this._clientBuilder = queueName => new QueueClient(new($"https://{config.Account}.queue.{suffix}/{queueName}"), config.GetAzureSasCredential());
                break;
            }

            case AzureQueuesConfig.AuthTypes.ManualTokenCredential:
            {
                this.ValidateAccountName(config.Account);
                var suffix = this.ValidateEndpointSuffix(config.EndpointSuffix);
                this._clientBuilder = queueName => new QueueClient(new($"https://{config.Account}.queue.{suffix}/{queueName}"), config.GetTokenCredential());
                break;
            }

            default:
                this._log.LogCritical("Azure Queue authentication type '{0}' undefined or not supported", config.Auth);
                throw new DocumentStorageException($"Azure Queue authentication type '{config.Auth}' undefined or not supported");
        }
    }

    /// <inheritdoc />
    public async Task<IQueue> ConnectToQueueAsync(string queueName, QueueOptions options = default, CancellationToken cancellationToken = default)
    {
        queueName = CleanQueueName(queueName);
        this._log.LogTrace("Connecting to queue name: {0}", queueName);

        ArgumentNullExceptionEx.ThrowIfNullOrWhiteSpace(queueName, nameof(queueName), "The queue name is empty");

        if (!string.IsNullOrEmpty(this._queueName))
        {
            this._log.LogError("The queue name has already been set, already connected to {0}", this._queueName);
            throw new InvalidOperationException($"The queue is already connected to `{this._queueName}`");
        }

        // Note: 3..63 chars, only lowercase letters, numbers and hyphens. No hyphens at start/end and no consecutive hyphens.
        this._queueName = queueName;
        this._log.LogDebug("Queue name: {0}", this._queueName);

        this._queue = this._clientBuilder(this._queueName);
        Response? result = await this._queue.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        this._log.LogTrace("Queue ready: status code {0}", result?.Status);

        this._poisonQueue = this._clientBuilder(this._queueName + this._config.PoisonQueueSuffix);
        result = await this._poisonQueue.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        this._log.LogTrace("Poison queue ready: status code {0}", result?.Status);

        if (options.DequeueEnabled)
        {
            this._log.LogTrace("Enabling dequeue on queue {0}, every {1} msecs", this._queueName, this._config.PollDelayMsecs);
            this._dispatchTimer = new Timer(this._config.PollDelayMsecs); // milliseconds
            this._dispatchTimer.Elapsed += this.DispatchMessages;
            this._dispatchTimer.Start();
        }

        return this;
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public void OnDequeue(Func<string, Task<bool>> processMessageAction)
    {
        this.Received += async (object sender, MessageEventArgs args) =>
        {
            ArgumentNullExceptionEx.ThrowIfNull(args.Message, nameof(args.Message), "The message received is NULL");
            QueueMessage message = args.Message;

            this._log.LogInformation("Message '{0}' received, expires at {1}", message.MessageId, message.ExpiresOn);

            try
            {
                if (message.DequeueCount <= this._config.MaxRetriesBeforePoisonQueue)
                {
                    bool success = await processMessageAction.Invoke(message.MessageText).ConfigureAwait(false);
                    if (success)
                    {
                        this._log.LogTrace("Message '{0}' successfully processed, deleting message", message.MessageId);
                        await this.DeleteMessageAsync(message, cancellationToken: default).ConfigureAwait(false);
                    }
                    else
                    {
                        var backoffDelay = TimeSpan.FromSeconds(1 * message.DequeueCount);
                        this._log.LogWarning("Message '{0}' failed to process, putting message back in the queue with a delay of {1} msecs",
                            message.MessageId, backoffDelay.TotalMilliseconds);
                        await this.UnlockMessageAsync(message, backoffDelay, cancellationToken: default).ConfigureAwait(false);
                    }
                }
                else
                {
                    this._log.LogError("Message '{0}' reached max attempts, moving to poison queue", message.MessageId);
                    await this.MoveMessageToPoisonQueueAsync(message, cancellationToken: default).ConfigureAwait(false);
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
                await this.UnlockMessageAsync(message, backoffDelay, cancellationToken: default).ConfigureAwait(false);
            }
#pragma warning restore CA1031
        };
    }

    /// <inheritdoc />
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
                Response<QueueMessage[]> receiveMessages = this._queue.ReceiveMessages(this._config.FetchBatchSize, visibilityTimeout: TimeSpan.FromSeconds(this._config.FetchLockSeconds));
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

    private async Task DeleteMessageAsync(QueueMessage message, CancellationToken cancellationToken)
    {
        await this._queue!.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken).ConfigureAwait(false);
    }

    private async Task UnlockMessageAsync(QueueMessage message, TimeSpan delay, CancellationToken cancellationToken)
    {
        await this._queue!.UpdateMessageAsync(message.MessageId, message.PopReceipt, visibilityTimeout: delay, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private async Task MoveMessageToPoisonQueueAsync(QueueMessage message, CancellationToken cancellationToken)
    {
        await this._poisonQueue!.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        var poisonMsg = new
        {
            message.MessageText,
            Id = message.MessageId,
            message.InsertedOn,
            message.DequeueCount,
        };

        var neverExpire = TimeSpan.FromSeconds(-1);
        await this._poisonQueue.SendMessageAsync(
            ToJson(poisonMsg),
            visibilityTimeout: TimeSpan.Zero,
            timeToLive: neverExpire, cancellationToken: cancellationToken).ConfigureAwait(false);
        await this.DeleteMessageAsync(message, cancellationToken).ConfigureAwait(false);
    }

    private void ValidateAccountName(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            this._log.LogCritical("The Azure Queue account name is empty");
            throw new DocumentStorageException("The account name is empty");
        }
    }

    private void ValidateAccountKey(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            this._log.LogCritical("The Azure Queue account key is empty");
            throw new DocumentStorageException("The Azure Queue account key is empty");
        }
    }

    private void ValidateConnectionString(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            this._log.LogCritical("The Azure Queue connection string is empty");
            throw new DocumentStorageException("The Azure Queue connection string is empty");
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
        return JsonSerializer.Serialize(data, indented ? s_indentedJsonOptions : s_notIndentedJsonOptions);
    }

    private static string CleanQueueName(string? name)
    {
        return name?.ToLowerInvariant().Replace('_', '-').Replace(' ', '-').Replace('.', '-') ?? string.Empty;
    }
}
