// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.KernelMemory;

public static class IndexExtensions
{
    public static string CleanName(string? name, string? defaultName)
    {
        // If appsettings / KM configuration is missing, we use "default" - See "DefaultIndexName" config.
        defaultName ??= "default";

        if (name == null) { return defaultName; }

        name = name.Trim();
        return string.IsNullOrEmpty(name) ? defaultName : name;
    }
}
