// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.Diagnostics;

namespace Microsoft.KernelMemory.Sources.DiscordBot;

/// <summary>
/// Service responsible for connecting to Discord, listening for messages
/// and generating events for Kernel Memory.
/// </summary>
public sealed class DiscordConnector : IHostedService, IDisposable, IAsyncDisposable
{
    private readonly DiscordSocketClient _client;
    private readonly IKernelMemory _memory;
    private readonly ILogger<DiscordConnector> _log;
    private readonly string _authToken;
    private readonly string _docStorageIndex;
    private readonly string _docStorageFilename;
    private readonly List<string> _pipelineSteps;

    /// <summary>
    /// New instance of Discord bot
    /// </summary>
    /// <param name="config">Discord settings</param>
    /// <param name="memory">Memory instance used to upload files when messages arrives</param>
    /// <param name="loggerFactory">App log factory</param>
    public DiscordConnector(
        DiscordConnectorConfig config,
        IKernelMemory memory,
        ILoggerFactory? loggerFactory = null)
    {
        this._log = (loggerFactory ?? DefaultLogger.Factory).CreateLogger<DiscordConnector>();
        this._authToken = config.DiscordToken;

        var dc = new DiscordSocketConfig
        {
            LogLevel = LogSeverity.Debug,
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent,
            LogGatewayIntentWarnings = true,
            SuppressUnknownDispatchWarnings = false
        };

        this._client = new DiscordSocketClient(dc);
        this._client.Log += this.OnLog;
        this._client.MessageReceived += this.OnMessage;
        this._memory = memory;
        this._docStorageIndex = config.Index;
        this._pipelineSteps = config.Steps;
        this._docStorageFilename = config.FileName;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await this._client.LoginAsync(TokenType.Bot, this._authToken).ConfigureAwait(false);
        await this._client.StartAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await this._client.LogoutAsync().ConfigureAwait(false);
        await this._client.StopAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        this._client.Dispose();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await this._client.DisposeAsync().ConfigureAwait(false);
    }

    #region private

    private static readonly Dictionary<LogSeverity, LogLevel> s_logLevels = new()
    {
        [LogSeverity.Critical] = LogLevel.Critical,
        [LogSeverity.Error] = LogLevel.Error,
        [LogSeverity.Warning] = LogLevel.Warning,
        [LogSeverity.Info] = LogLevel.Information,
        [LogSeverity.Verbose] = LogLevel.Debug, // note the inconsistency
        [LogSeverity.Debug] = LogLevel.Trace // note the inconsistency
    };

    private Task OnMessage(SocketMessage message)
    {
        var msg = new DiscordMessage
        {
            MessageId = message.Id.ToString(CultureInfo.InvariantCulture),
            AuthorId = message.Author.Id.ToString(CultureInfo.InvariantCulture),
            ChannelId = message.Channel.Id.ToString(CultureInfo.InvariantCulture),
            ReferenceMessageId = message.Reference?.MessageId.ToString() ?? string.Empty,
            AuthorUsername = message.Author.Username,
            ChannelName = message.Channel.Name,
            Timestamp = message.Timestamp,
            Content = message.Content,
            CleanContent = message.CleanContent,
            EmbedsCount = message.Embeds.Count,
        };

        if (message.Channel is SocketTextChannel textChannel)
        {
            msg.ChannelMention = textChannel.Mention;
            msg.ChannelTopic = textChannel.Topic;
            msg.ServerId = textChannel.Guild.Id.ToString(CultureInfo.InvariantCulture);
            msg.ServerName = textChannel.Guild.Name;
            msg.ServerDescription = textChannel.Guild.Description;
            msg.ServerMemberCount = textChannel.Guild.MemberCount;
        }

        this._log.LogTrace("[{0}] New message from '{1}' [{2}]", msg.MessageId, msg.AuthorUsername, msg.AuthorId);
        this._log.LogTrace("[{0}] Channel: {1}", msg.MessageId, msg.ChannelId);
        this._log.LogTrace("[{0}] Channel: {1}", msg.MessageId, msg.ChannelName);
        this._log.LogTrace("[{0}] Timestamp: {1}", msg.MessageId, msg.Timestamp);
        this._log.LogTrace("[{0}] Content: {1}", msg.MessageId, msg.Content);
        this._log.LogTrace("[{0}] CleanContent: {1}", msg.MessageId, msg.CleanContent);
        this._log.LogTrace("[{0}] Reference: {1}", msg.MessageId, msg.ReferenceMessageId);
        this._log.LogTrace("[{0}] EmbedsCount: {1}", msg.MessageId, msg.EmbedsCount);
        if (message.Embeds.Count > 0)
        {
            foreach (Embed? x in message.Embeds)
            {
                if (x == null) { continue; }

                this._log.LogTrace("[{0}] Embed Title: {1}", message.Id, x.Title);
                this._log.LogTrace("[{0}] Embed Url: {1}", message.Id, x.Url);
                this._log.LogTrace("[{0}] Embed Description: {1}", message.Id, x.Description);
            }
        }

        Task.Run(async () =>
        {
            string documentId = $"{msg.ServerId}_{msg.ChannelId}_{msg.MessageId}";
            string content = JsonSerializer.Serialize(msg);
            Stream fileContent = new MemoryStream(Encoding.UTF8.GetBytes(content), false);
            await using (fileContent.ConfigureAwait(false))
            {
                await this._memory.ImportDocumentAsync(
                    fileContent,
                    fileName: this._docStorageFilename,
                    documentId: documentId,
                    index: this._docStorageIndex,
                    steps: this._pipelineSteps).ConfigureAwait(false);
            }
        });

        return Task.CompletedTask;
    }

    private Task OnLog(LogMessage msg)
    {
        var logLevel = LogLevel.Information;
        if (s_logLevels.TryGetValue(msg.Severity, out LogLevel value))
        {
            logLevel = value;
        }

        this._log.Log(logLevel, "{0}: {1}", msg.Source, msg.Message);

        return Task.CompletedTask;
    }

    #endregion
}
