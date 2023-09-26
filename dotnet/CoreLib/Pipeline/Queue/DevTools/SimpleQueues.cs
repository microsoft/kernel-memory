// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticMemory.Diagnostics;
using Timer = System.Timers.Timer;

namespace Microsoft.SemanticMemory.Pipeline.Queue.DevTools;

/// <summary>
/// Basic implementation of a file based queue for local testing.
/// This is not meant for production scenarios, only to avoid spinning up additional services.
/// </summary>
public sealed class SimpleQueues : IQueue
{
    private sealed class MessageEventArgs : EventArgs
    {
        public string Filename { get; set; } = string.Empty;
    }

    /// <summary>
    /// Event triggered when a message is received
    /// </summary>
    private event EventHandler<MessageEventArgs>? Received;

    // Extension of the files containing the messages. Don't leave this empty, it's better
    // filtering and it mitigates the risk of unwanted file deletions.
    private const string FileExt = ".msg";

    // Parent directory of the directory containing messages
    private readonly string _directory;

    // Sorted list of messages (the key is the file path)
    private readonly SortedSet<string> _messages = new();

    // List of messages being processed (the key is the file path)
    private readonly HashSet<string> _processingMessages = new();

    // Lock helpers
    private readonly object _lock = new();
    private bool _busy = false;

    // Name of the queue, used also as a directory name
    private string _queueName = string.Empty;

    // Full queue directory path
    private string _queuePath = string.Empty;

    // Timer triggering the filesystem read
    private Timer? _populateTimer;

    // Timer triggering the message dispatch
    private Timer? _dispatchTimer;

    // Application logger
    private readonly ILogger<SimpleQueues> _log;

    /// <summary>
    /// Create new file based queue
    /// </summary>
    /// <param name="config">File queue configuration</param>
    /// <param name="log">Application logger</param>
    /// <exception cref="InvalidOperationException"></exception>
    public SimpleQueues(SimpleQueuesConfig config, ILogger<SimpleQueues>? log = null)
    {
        this._log = log ?? DefaultLogger<SimpleQueues>.Instance;
        if (!Directory.Exists(config.Directory))
        {
            Directory.CreateDirectory(config.Directory);
        }

        this._directory = config.Directory;
    }

    /// <inherit />
    public Task<IQueue> ConnectToQueueAsync(string queueName, QueueOptions options = default, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(queueName))
        {
            throw new ArgumentOutOfRangeException(nameof(queueName), "The queue name is empty");
        }

        if (!string.IsNullOrEmpty(this._queueName))
        {
            throw new InvalidOperationException($"The queue is already connected to `{this._queueName}`");
        }

        this._queuePath = Path.Join(this._directory, queueName);

        if (!Directory.Exists(this._queuePath))
        {
            Directory.CreateDirectory(this._queuePath);
        }

        this._queueName = queueName;

        if (options.DequeueEnabled)
        {
            this._populateTimer = new Timer(250); // milliseconds
            this._populateTimer.Elapsed += this.PopulateQueue;
            this._populateTimer.Start();

            this._dispatchTimer = new Timer(100); // milliseconds
            this._dispatchTimer.Elapsed += this.DispatchMessages;
            this._dispatchTimer.Start();
        }

        return Task.FromResult<IQueue>(this);
    }

    /// <inherit />
    public async Task EnqueueAsync(string message, CancellationToken cancellationToken = default)
    {
        // Use a sortable file name. Don't use UTC for local development.
        var messageId = DateTimeOffset.Now.ToString("yyyyMMdd.HHmmss.fffffff", CultureInfo.InvariantCulture)
                        + "." + Guid.NewGuid().ToString("N");
        var file = Path.Join(this._queuePath, $"{messageId}{FileExt}");
        await File.WriteAllTextAsync(file, message, cancellationToken).ConfigureAwait(false);

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

                string message = await File.ReadAllTextAsync(args.Filename).ConfigureAwait(false);
                bool success = await processMessageAction.Invoke(message).ConfigureAwait(false);
                if (success)
                {
                    this.DeleteMessage(args.Filename);
                }
                else
                {
                    this._log.LogWarning("Message '{0}' processing failed with exception, putting message back in the queue", args.Filename);
                    this.UnlockMessage(args.Filename);
                }
            }
#pragma warning disable CA1031 // Must catch all to handle queue properly
            catch (Exception e)
            {
                // Exceptions caught by this block:
                // - message processing failed with exception
                // - failed to delete message from disk
                this._log.LogWarning(e, "Message '{0}' processing failed with exception, putting message back in the queue", args.Filename);
                this.UnlockMessage(args.Filename);
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

        lock (this._lock)
        {
            this._busy = true;
            this._log.LogTrace("Populating queue");
            try
            {
                DirectoryInfo d = new(this._queuePath);
                FileInfo[] files = d.GetFiles($"*{FileExt}");
                foreach (FileInfo f in files)
                {
                    // This check is not strictly required, only used to reduce logging statements
                    if (!this._messages.Contains(f.FullName))
                    {
                        this._log.LogTrace("Found file {0}", f.FullName);
                        this._messages.Add(f.FullName);
                    }
                }
            }
            catch (Exception e)
            {
                this._log.LogError(e, "Fetch failed");
                throw;
            }
            finally
            {
                this._busy = false;
            }
        }
    }

    private void DispatchMessages(object? sender, ElapsedEventArgs e)
    {
        if (this._busy || this._messages.Count == 0)
        {
            return;
        }

        lock (this._lock)
        {
            this._busy = true;
            this._log.LogTrace("Dispatching {0} messages", this._messages.Count);
            try
            {
                var messages = this._messages;
                foreach (var filename in messages)
                {
                    if (this.LockMessage(filename))
                    {
                        this.Received?.Invoke(this, new MessageEventArgs { Filename = filename });
                    }
                    else
                    {
                        this._log.LogTrace("Skipping message {0} since it is already being processed", filename);
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
            }
        }
    }

    private bool LockMessage(string filename)
    {
        return this._processingMessages.Add(filename);
    }

    private void UnlockMessage(string filename)
    {
        this._processingMessages.Remove(filename);
    }

    private void DeleteMessage(string filename)
    {
        this._log.LogTrace("Deleting message from memory {0}", filename);
        this._messages.Remove(filename);
        this.UnlockMessage(filename);

        if (!File.Exists(filename) || !filename.EndsWith(FileExt, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        this._log.LogTrace("Deleting file from disk {0}", filename);
        File.Delete(filename);
    }
}
