// Copyright (c) Microsoft. All rights reserved.
namespace KernelMemory.Main.CLI.Models;

/// <summary>
/// Cache configuration information.
/// </summary>
public class CacheConfigDto
{
    public string Type { get; init; } = string.Empty;
    public string? Path { get; init; }
}
