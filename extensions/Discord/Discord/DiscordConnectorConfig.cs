// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;

namespace Microsoft.KernelMemory.Sources.DiscordBot;

/// <summary>
/// Discord bot settings
/// </summary>
public class DiscordConnectorConfig
{
    /// <summary>
    /// Discord bot authentication token
    /// </summary>
    public string DiscordToken { get; set; } = string.Empty;

    /// <summary>
    /// Index where to store files (not memories)
    /// </summary>
    public string Index { get; set; } = "discord";

    /// <summary>
    /// File name used when uploading a message.
    /// </summary>
    public string FileName { get; set; } = "discord.json";

    /// <summary>
    /// Handlers processing the incoming Discord events
    /// </summary>
    public List<string> Steps { get; set; } = [];
}
