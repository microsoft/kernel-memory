// Copyright (c) Microsoft. All rights reserved.

namespace KernelMemory.Main.CLI.Infrastructure;

/// <summary>
/// Service that holds the configuration file path.
/// Registered in DI to allow commands to access the config path.
/// </summary>
public sealed class ConfigPathService
{
    public ConfigPathService(string path)
    {
        this.Path = path;
    }

    public string Path { get; }
}
