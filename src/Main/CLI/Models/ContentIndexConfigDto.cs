// Copyright (c) Microsoft. All rights reserved.
namespace KernelMemory.Main.CLI.Models;

/// <summary>
/// Content index configuration information.
/// </summary>
public class ContentIndexConfigDto
{
    public string Type { get; init; } = string.Empty;
    public string? Path { get; init; }
}
