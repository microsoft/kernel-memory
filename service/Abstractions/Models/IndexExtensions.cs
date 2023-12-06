// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.KernelMemory;

public static class IndexExtensions
{
    public static string CleanName(string? name)
    {
        if (name == null) { return Constants.DefaultIndex; }

        name = name.Trim();
        return string.IsNullOrEmpty(name) ? Constants.DefaultIndex : name;
    }
}
