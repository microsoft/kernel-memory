// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.KernelMemory;

public static class IndexExtensions
{
    public static string CleanName(string? name, string defaultValue)
    {
        var indexName = (string.IsNullOrWhiteSpace(name) ? defaultValue : name)?.Trim();
        return string.IsNullOrEmpty(indexName) ? Constants.DefaultIndex : indexName!;
    }
}
