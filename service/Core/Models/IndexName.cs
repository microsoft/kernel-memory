// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.KernelMemory.Models;

[Experimental("KMEXP00")]
public static class IndexName
{
    /// <summary>
    /// Clean the index name, returning a non empty value if possible
    /// </summary>
    /// <param name="name">Input index name</param>
    /// <param name="defaultName">Default value to fall back when input is empty</param>
    /// <returns>Non empty index name</returns>
    public static string CleanName(string? name, string? defaultName)
    {
        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(defaultName))
        {
            throw new ArgumentNullException(nameof(defaultName),
                "Both index name and default fallback value are empty. Provide an index name or a default value to use when the index name is empty.");
        }

        defaultName = defaultName?.Trim() ?? string.Empty;
        if (name == null) { return defaultName; }

        name = name.Trim();
        return string.IsNullOrWhiteSpace(name) ? defaultName : name;
    }
}
