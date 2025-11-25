// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.FileSystem.DevTools;
using Timer = System.Timers.Timer;

namespace Microsoft.KernelMemory.Pipeline.Queue.DevTools;

/// <summary>
/// Basic implementation of a file based queue for local testing.
/// This is not meant for production scenarios, only to avoid spinning up additional services.
/// </summary>
[Experimental("KMEXP04")]
#pragma warning disable CA1031 // need to log all errors
public sealed class SimpleQueues : IQueue
{
    private readonly SimpleQueuesConfig _config;

    private sealed class MessageEventArgs : EventArgs
    {
        public Message? Message { get; set; }
    }

    /// <summary>
    /// Event triggered when a message is received
    /// TODO: move to async events
    /// </summary>
    private event EventHandler<MessageEventArgs>? Received;

    // Extension of the files containing the messages. Don't leave this empty, it's better
    // filtering and it mitigates the risk of unwanted file deletions.
    private const string FileExt = ".sqm.json";

    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    // Lock helpers. This is static so that processes sharing the same storage don't conflict with each other.
    private static readonly SemaphoreSlim s_lock = new(initialCount: 1, maxCount: 1);

    // Underlying storage where messages and queues are stored
    private readonly IFileSystem _fileSystem;

    // Application logger
    private readonly ILogger<SimpleQueues> _log;

    private readonly ConcurrentQueue<Message> _queue = new();

    // Max attempts at processing each message
    private readonly int _maxAttempts;

    private readonly CancellationTokenSource _cancellation;

    // Name of the queue, used also as a directory name
    private string _queueName = string.Empty;

    // Name of the poison queue, used also as a directory name
    private string _poisonQueueName = string.Empty;

    // Timer triggering the filesystem read
    private Timer? _populateTimer;

    // Timer triggering the message dispatch
    private Timer? _dispatchTimer;

    /// <summary>
    /// Create new file based queue
    /// </summary>
    /// <param name="config">File queue configuration</param>
    /// <param name="loggerFactory">Application logger factory</param>
    /// <exception cref="InvalidOperationException"></exception>
    public SimpleQueues(SimpleQueuesConfig config, ILoggerFactory? loggerFactory = null)
    {
        config.Validate();
        this._config = config;

        this._log = (loggerFactory ?? DefaultLogger.Factory).CreateLogger<SimpleQueues>();
        this._cancellation = new CancellationTokenSource();

        switch (config.StorageType)
        {
            case FileSystemTypes.Disk:
                this._log.LogTrace("Using {StorageType} storage", nameof(DiskFileSystem));
                this._fileSystem = new DiskFileSystem(config.Directory, null, loggerFactory);
                break;

            case FileSystemTypes.Volatile:
                this._log.LogTrace("Using {StorageType} storage", nameof(VolatileFileSystem));
                this._fileSystem = VolatileFileSystem.GetInstance(config.Directory, null, loggerFactory);
                break;

            default:
                this._log.LogCritical("Unknown storage type {StorageType}", config.StorageType);
                throw new ArgumentException($"Unknown storage type {config.StorageType}");
        }

        this._maxAttempts = config.MaxRetriesBeforePoisonQueue + 1;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        this._cancellation?.Cancel();
        this._populateTimer?.Dispose();
        this._dispatchTimer?.Dispose();
        this._cancellation?.Dispose();
    }

    /// <inheritdoc />
    public async Task<IQueue> ConnectToQueueAsync(string queueName, QueueOptions options = default, CancellationToken cancellationToken = default)
    {
        ArgumentNullExceptionEx.ThrowIfNullOrWhiteSpace(queueName, nameof(queueName), "The queue name is empty");

        if (queueName == this._queueName) { return this; }

        if (!string.IsNullOrEmpty(this._queueName))
        {
            this._log.LogCritical("The client is already connected to queue {QueueName}", this._queueName);
            throw new InvalidOperationException($"The queue is already connected to `{this._queueName}`");
        }

        this._queueName = queueName;
        this._poisonQueueName = $"{queueName}{this._config.PoisonQueueSuffix}";
        await this.CreateDirectoriesAsync(cancellationToken).ConfigureAwait(false);

        this._log.LogTrace("Client connected to queue {QueueName} and poison queue {PoisonQueueName}", this._queueName, this._poisonQueueName);

        if (options.DequeueEnabled)
        {
            this._populateTimer = new Timer(this._config.PollDelayMsecs);
            this._populateTimer.Elapsed += this.PopulateQueue;
            this._populateTimer.Start();

            this._dispatchTimer = new Timer(this._config.DispatchFrequencyMsecs);
            this._dispatchTimer.Elapsed += this.DispatchMessage;
            this._dispatchTimer.Start();

            this._log.LogTrace("Queue {QueueName}: polling and dispatching timers created", this._queueName);
        }
        else
        {
            this._log.LogTrace("Queue {QueueName}: dequeue not enabled", this._queueName);
        }

        return this;
    }

    /// <inheritdoc />
    public async Task EnqueueAsync(string message, CancellationToken cancellationToken = default)
    {
        // Use a sortable file name. Don't use UTC for local development.
        var messageId = DateTimeOffset.Now.ToString("yyyyMMdd.HHmmss.fffffff", CultureInfo.InvariantCulture)
                        + "." + Guid.NewGuid().ToString("N");

        await this.StoreMessageAsync(
            this._queueName,
            new Message
            {
                Id = messageId,
                Content = message,
                DequeueCount = 0,
                Schedule = DateTimeOffset.UtcNow
            },
            cancellationToken).ConfigureAwait(false);

        this._log.LogInformation("Queue {QueueName}: message {MessageId} sent", this._queueName, messageId);
    }

    /// <inheritdoc />
    /// <see cref="DistributedPipelineOrchestrator.AddHandlerAsync"/> about the logic handling dequeued messages.
    public void OnDequeue(Func<string, Task<ReturnType>> processMessageAction)
    {
        this._log.LogInformation("Queue {QueueName}: subscribing...", this._queueName);
        this.Received += async (sender, args) =>
        {
            Message message = new();
            var retry = false;
            var poison = false;
            try
            {
                ArgumentNullExceptionEx.ThrowIfNull(args.Message, nameof(args.Message), "The message received is NULL");
                message = args.Message;

                this._log.LogInformation("Queue {QueueName}: message {MessageId} received", this._queueName, message.Id);

                // Process message with the logic provided by the orchestrator
                var returnType = await processMessageAction.Invoke(message.Content).ConfigureAwait(false);
                switch (returnType)
                {
                    case ReturnType.Success:
                        this._log.LogTrace("Message '{MessageId}' successfully processed, deleting message", message.Id);
                        await this.DeleteMessageAsync(message.Id, this._cancellation.Token).ConfigureAwait(false);
                        break;

                    case ReturnType.TransientError:
                        message.LastError = "Message handler returned false";
                        if (message.DequeueCount == this._maxAttempts)
                        {
                            this._log.LogError("Message '{MessageId}' processing failed to process, max attempts reached, moving to poison queue. Message content: {MessageContent}", message.Id, message.Content);
                            poison = true;
                        }
                        else
                        {
                            this._log.LogWarning("Message '{MessageId}' failed to process, putting message back in the queue. Message content: {MessageContent}", message.Id, message.Content);
                            retry = true;
                        }

                        break;

                    case ReturnType.FatalError:
                        this._log.LogError("Message '{MessageId}' failed to process due to a non-recoverable error, moving to poison queue", message.Id);
                        poison = true;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException($"Unknown {returnType:G} result");
                }
            }
            catch (KernelMemoryException e) when (e.IsTransient.HasValue && !e.IsTransient.Value)
            {
                message.LastError = $"{e.GetType().FullName} [{e.InnerException?.GetType().FullName}]: {e.Message}";
                this._log.LogError(e, "Message '{MessageId}' failed to process due to a non-recoverable error, moving to poison queue.", message.Id);
                poison = true;
            }
            // Note: must catch all also because using a void event handler
            catch (Exception e)
            {
                message.LastError = $"{e.GetType().FullName}: {e.Message}";
                if (message.DequeueCount == this._maxAttempts)
                {
                    this._log.LogError(e, "Message '{MessageId}' processing failed with exception, max attempts reached, moving to poison queue. Message content: {MessageContent}.", message.Id, message.Content);
                    poison = true;
                }
                else
                {
                    this._log.LogWarning(e, "Message '{MessageId}' processing failed with exception, putting message back in the queue. Message content: {MessageContent}.", message.Id, message.Content);
                    retry = true;
                }
            }

            message.Unlock();
            if (retry)
            {
                var backoffDelay = TimeSpan.FromSeconds(1 * message.DequeueCount);
                message.RunIn(backoffDelay);
                await this.StoreMessageAsync(this._queueName, message, this._cancellation.Token).ConfigureAwait(false);
            }
            else if (poison)
            {
                await this.StoreMessageAsync(this._poisonQueueName, message, this._cancellation.Token).ConfigureAwait(false);
                await this.DeleteMessageAsync(message.Id, this._cancellation.Token).ConfigureAwait(false);
            }
        };
    }

    /// <summary>
    /// Read messages from the file system and store the in memory, ready to be dispatched.
    /// Use a lock to avoid unnecessary file system reads.
    /// </summary>
    private void PopulateQueue(object? sender, ElapsedEventArgs elapsedEventArgs)
    {
        Task.Run(async () =>
        {
            var lockAcquired = false;
            try
            {
                if (this._queue.Count >= this._config.FetchBatchSize) { return; }

                await s_lock.WaitAsync(this._cancellation.Token).ConfigureAwait(false);
                lockAcquired = true;

                // Loop through all messages on storage
                var messagesOnStorage = (await this._fileSystem.GetAllFileNamesAsync(this._queueName, "", this._cancellation.Token).ConfigureAwait(false)).ToList();
                if (messagesOnStorage.Count == 0) { return; }

                this._log.LogTrace("Queue {QueueName}: {MsgCountOnStorage} messages on storage, {MsgCountReady} ready to dispatch, max batch size {FetchBatchSize}",
                    this._queueName, messagesOnStorage.Count, this._queue.Count, this._config.FetchBatchSize);

                foreach (var fileName in messagesOnStorage)
                {
                    // Limit the number of messages loaded in memory
                    if (this._queue.Count >= this._config.FetchBatchSize)
                    {
                        this._log.LogTrace("Queue {QueueName}: max batch size {FetchBatchSize} reached", this._queueName, this._config.FetchBatchSize);
                        return;
                    }

                    // Ignore files that are not messages
                    if (!fileName.EndsWith(FileExt, StringComparison.OrdinalIgnoreCase)) { continue; }

                    // Load message from storage
                    var messageId = fileName.Substring(0, fileName.Length - FileExt.Length);
                    var message = await this.ReadMessageAsync(messageId, this._cancellation.Token).ConfigureAwait(false);

                    // Avoid enqueueing the same message twice, even if not locked, to avoid double execution
                    if (message.IsTimeToRun() && !message.IsLocked() && this._queue.All(x => x.Id != messageId))
                    {
                        // Update message metadata
                        message.Lock(this._config.FetchLockSeconds);
                        message.DequeueCount++;
                        await this.StoreMessageAsync(this._queueName, message, this._cancellation.Token).ConfigureAwait(false);

                        // Add to list of messages to be processed
                        this._queue.Enqueue(message);
                        this._log.LogTrace("Queue {QueueName}: found message {MessageId}", this._queueName, messageId);
                    }

                    if (this._log.IsEnabled(LogLevel.Trace))
                    {
                        if (!message.IsTimeToRun())
                        {
                            this._log.LogTrace("Queue {QueueName}: skipping message {MessageId} scheduled in the future", this._queueName, messageId);
                        }
                        else if (message.IsLocked())
                        {
                            this._log.LogTrace("Queue {QueueName}: skipping message {MessageId} because it is locked", this._queueName, messageId);
                        }
                        else if (this._queue.Any(x => x.Id == messageId))
                        {
                            this._log.LogTrace("Queue {QueueName}: skipping message {MessageId} because it is already loaded", this._queueName, messageId);
                        }
                    }
                }
            }
            catch (DirectoryNotFoundException e)
            {
                this._log.LogError(e, "Directory missing, recreating.");
                await this.CreateDirectoriesAsync(this._cancellation.Token).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                this._log.LogError(e, "Queue {QueueName}: Unexpected error while polling.", this._queueName);
            }
            finally
            {
                // Decrease the internal counter only it the lock was acquired,
                // e.g. not when WaitAsync times out or throws some exception
                if (lockAcquired)
                {
                    s_lock.Release();
                }
            }
        }, this._cancellation.Token);
    }

    /// <summary>
    /// Dispatch messages in memory, previously loaded from file system by <see cref="PopulateQueue"/>.
    /// Use a lock to avoid dispatching the same messages more than once.
    /// <see cref="OnDequeue"/> to track how messages flow externally.
    /// </summary>
    private void DispatchMessage(object? sender, ElapsedEventArgs e)
    {
        Task.Run(async () =>
        {
            var lockAcquired = false;
            try
            {
                if (this._queue.IsEmpty) { return; }

                await s_lock.WaitAsync(this._cancellation.Token).ConfigureAwait(false);
                lockAcquired = true;

                this._log.LogTrace("Dispatching {MessageCount} messages", this._queue.Count);

                while (this._queue.TryDequeue(out Message? message))
                {
                    this.Received?.Invoke(this, new MessageEventArgs { Message = message });
                }
            }
            catch (Exception ex)
            {
                this._log.LogError(ex, "Queue {QueueName}: Unexpected error while dispatching", this._queueName);
            }
            finally
            {
                // Decrease the internal counter only it the lock was acquired,
                // e.g. not when WaitAsync times out or throws some exception
                if (lockAcquired)
                {
                    s_lock.Release();
                }
            }
        }, this._cancellation.Token);
    }

    private static string Serialize(Message msg) { return JsonSerializer.Serialize(msg, s_jsonOptions); }

    private static Message Deserialize(string json) { return JsonSerializer.Deserialize<Message>(json) ?? new Message(); }

    private async Task<Message> ReadMessageAsync(string id, CancellationToken cancellationToken = default)
    {
        this._log.LogTrace("Queue {QueueName}: reading message {MessageId}", this._queueName, id);
        var serializedMsg = await this._fileSystem.ReadFileAsTextAsync(
            volume: this._queueName, relPath: "", fileName: $"{id}{FileExt}", cancellationToken: cancellationToken).ConfigureAwait(false);
        return Deserialize(serializedMsg);
    }

    private async Task StoreMessageAsync(string queueName, Message message, CancellationToken cancellationToken = default)
    {
        this._log.LogTrace("Queue {QueueName}: storing message {MessageId}", this._queueName, message.Id);
        await this._fileSystem.WriteFileAsync(queueName, "", $"{message.Id}{FileExt}", Serialize(message), cancellationToken).ConfigureAwait(false);
    }

    private async Task DeleteMessageAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            this._log.LogTrace("Queue {QueueName}: deleting message {MessageId}", this._queueName, id);
            var fileName = $"{id}{FileExt}";
            this._log.LogTrace("Deleting file from storage {FileName}", fileName);
            await this._fileSystem.DeleteFileAsync(this._queueName, "", fileName, cancellationToken).ConfigureAwait(false);
        }
        catch (DirectoryNotFoundException)
        {
            await this.CreateDirectoriesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            this._log.LogWarning(e, "Error while deleting message from storage");
        }
    }

    private async Task CreateDirectoriesAsync(CancellationToken cancellationToken = default)
    {
        await this._fileSystem.CreateVolumeAsync(this._queueName, cancellationToken).ConfigureAwait(false);
        await this._fileSystem.CreateVolumeAsync(this._poisonQueueName, cancellationToken).ConfigureAwait(false);
    }
}

#pragma warning restore CA1031
