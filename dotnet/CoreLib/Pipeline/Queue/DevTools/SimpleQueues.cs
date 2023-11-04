﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
public sealed class SimpleQueues : IQueue
{
    private sealed class MessageEventArgs : EventArgs
    {
        public string MessageId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Event triggered when a message is received
    /// </summary>
    private event EventHandler<MessageEventArgs>? Received;

    /// <summary>
    /// How often to check the file system for new messages
    /// </summary>
    private const int PollFrequencyMsecs = 250;

    /// <summary>
    /// How often to dispatch messages in the queue
    /// </summary>
    private const int DispatchFrequencyMsecs = 100;

    // Extension of the files containing the messages. Don't leave this empty, it's better
    // filtering and it mitigates the risk of unwanted file deletions.
    private const string FileExt = ".msg";

    // Lock helpers
    private static readonly SemaphoreSlim s_lock = new(initialCount: 1, maxCount: 1);
    private bool _busy = false;

    // Underlying storage where messages and queues are stored
    private readonly IFileSystem _fileSystem;

    // Application logger
    private readonly ILogger<SimpleQueues> _log;

    // Sorted list of messages (the key is the file path)
    private readonly SortedSet<string> _messages = new();

    // List of messages being processed (the key is the file path)
    private readonly HashSet<string> _processingMessages = new();

    // Name of the queue, used also as a directory name
    private string _queueName = string.Empty;

    // Timer triggering the filesystem read
    private Timer? _populateTimer;

    // Timer triggering the message dispatch
    private Timer? _dispatchTimer;

    /// <summary>
    /// Create new file based queue
    /// </summary>
    /// <param name="config">File queue configuration</param>
    /// <param name="log">Application logger</param>
    /// <exception cref="InvalidOperationException"></exception>
    public SimpleQueues(SimpleQueuesConfig config, ILogger<SimpleQueues>? log = null)
    {
        this._log = log ?? DefaultLogger<SimpleQueues>.Instance;
        switch (config.StorageType)
        {
            case FileSystemTypes.Disk:
                this._fileSystem = new DiskFileSystem(config.Directory, this._log);
                break;

            case FileSystemTypes.Volatile:
                this._fileSystem = VolatileFileSystem.GetInstance(this._log);
                break;

            default:
                throw new ArgumentException($"Unknown storage type {config.StorageType}");
        }
    }

    /// <inherit />
    public async Task<IQueue> ConnectToQueueAsync(string queueName, QueueOptions options = default, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(queueName))
        {
            throw new ArgumentOutOfRangeException(nameof(queueName), "The queue name is empty");
        }

        if (!string.IsNullOrEmpty(this._queueName))
        {
            throw new InvalidOperationException($"The queue is already connected to `{this._queueName}`");
        }

        this._queueName = queueName;
        await this._fileSystem.CreateVolumeAsync(this._queueName, cancellationToken).ConfigureAwait(false);

        if (options.DequeueEnabled)
        {
            this._populateTimer = new Timer(PollFrequencyMsecs);
            this._populateTimer.Elapsed += this.PopulateQueue;
            this._populateTimer.Start();

            this._dispatchTimer = new Timer(DispatchFrequencyMsecs);
            this._dispatchTimer.Elapsed += this.DispatchMessages;
            this._dispatchTimer.Start();
        }

        return this;
    }

    /// <inherit />
    public async Task EnqueueAsync(string message, CancellationToken cancellationToken = default)
    {
        // Use a sortable file name. Don't use UTC for local development.
        var messageId = DateTimeOffset.Now.ToString("yyyyMMdd.HHmmss.fffffff", CultureInfo.InvariantCulture)
                        + "." + Guid.NewGuid().ToString("N");

        await this._fileSystem.WriteFileAsync(this._queueName, "", $"{messageId}{FileExt}", message, cancellationToken).ConfigureAwait(false);

        this._log.LogInformation("Message sent");
    }

    /// <inherit />
    public void OnDequeue(Func<string, Task<bool>> processMessageAction)
    {
        this.Received += async (sender, args) =>
        {
            try
            {
                this._log.LogInformation("Message received");

                string message = await this._fileSystem.ReadFileAsTextAsync(this._queueName, "", $"{args.MessageId}{FileExt}").ConfigureAwait(false);
                bool success = await processMessageAction.Invoke(message).ConfigureAwait(false);
                if (success)
                {
                    await this.DeleteMessageAsync(args.MessageId).ConfigureAwait(false);
                }
                else
                {
                    this._log.LogWarning("Message '{0}' processing failed, putting message back in the queue", args.MessageId);
                    this.UnlockMessage(args.MessageId);
                }
            }
#pragma warning disable CA1031 // Must catch all to handle queue properly
            catch (Exception e)
            {
                // Exceptions caught by this block:
                // - message processing failed with exception
                // - failed to delete message from disk
                this._log.LogWarning(e, "Message '{0}' processing failed with exception, putting message back in the queue", args.MessageId);
                this.UnlockMessage(args.MessageId);
            }
#pragma warning restore CA1031
        };
    }

    /// <inherit />
    public void Dispose()
    {
        this._populateTimer?.Dispose();
        this._dispatchTimer?.Dispose();
    }

    private void PopulateQueue(object? sender, ElapsedEventArgs elapsedEventArgs)
    {
        if (this._busy)
        {
            return;
        }

#pragma warning disable CA1031 // need to log all errors
        Task.Run(async () =>
        {
            await s_lock.WaitAsync().ConfigureAwait(false);
            this._busy = true;
            try
            {
                this._log.LogTrace("Populating queue {0}", this._queueName);
                var messages = (await this._fileSystem.GetAllFileNamesAsync(this._queueName, "").ConfigureAwait(false)).ToList();
                this._log.LogTrace("Queue {0}: {1} messages on disk", this._queueName, messages.Count);
                foreach (var fileName in messages)
                {
                    if (!fileName.EndsWith(FileExt, StringComparison.OrdinalIgnoreCase)) { continue; }

                    var messageId = fileName.Substring(0, fileName.Length - FileExt.Length);

                    // This check is not strictly required, only used to reduce logging statements
                    if (!this._messages.Contains(messageId))
                    {
                        this._log.LogTrace("Found message {0}", messageId);
                        this._messages.Add(messageId);
                    }
                }
            }
            catch (DirectoryNotFoundException e)
            {
                this._log.LogError(e, "Directory missing, recreating");
                await this._fileSystem.CreateVolumeAsync(this._queueName).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                this._log.LogError(e, "Unexpected error while polling the queue");
            }
            finally
            {
                this._busy = false;
                s_lock.Release();
            }
        });
#pragma warning restore CA1031
    }

    private void DispatchMessages(object? sender, ElapsedEventArgs e)
    {
        if (this._busy || this._messages.Count == 0)
        {
            return;
        }

        Task.Run(async () =>
        {
            await s_lock.WaitAsync().ConfigureAwait(false);
            this._busy = true;
            this._log.LogTrace("Dispatching {0} messages", this._messages.Count);
            try
            {
                // Copy the list to avoid errors when the original collection is modified elsewhere
                List<string> messages = this._messages.ToList();
                foreach (var messageId in messages)
                {
                    if (this.LockMessage(messageId))
                    {
                        this.Received?.Invoke(this, new MessageEventArgs { MessageId = messageId });
                    }
                    else
                    {
                        this._log.LogTrace("Skipping message {0} since it is already being processed", messageId);
                    }
                }
            }
            catch (Exception exception)
            {
                this._log.LogError(exception, "Dispatch failed");
                throw;
            }
            finally
            {
                this._busy = false;
                s_lock.Release();
            }
        });
    }

    private bool LockMessage(string messageId)
    {
        return this._processingMessages.Add(messageId);
    }

    private void UnlockMessage(string messageId)
    {
        this._processingMessages.Remove(messageId);
    }

    private async Task DeleteMessageAsync(string messageId)
    {
        this._log.LogTrace("Deleting message from queue {0}", messageId);
        this._messages.Remove(messageId);
        this.UnlockMessage(messageId);

        var fileName = $"{messageId}{FileExt}";
        this._log.LogTrace("Deleting file from disk {0}", fileName);
        await this._fileSystem.DeleteFileAsync(this._queueName, "", fileName).ConfigureAwait(false);
    }
}
