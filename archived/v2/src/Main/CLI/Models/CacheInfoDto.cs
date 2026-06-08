// Copyright (c) Microsoft. All rights reserved.
namespace KernelMemory.Main.CLI.Models;

/// <summary>
/// Information about cache configuration.
/// </summary>
public class CacheInfoDto
{
    public CacheConfigDto? EmbeddingsCache { get; init; }
    public CacheConfigDto? LlmCache { get; init; }
}
