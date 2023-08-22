// Copyright (c) Microsoft. All rights reserved.

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.SemanticMemory;

public static class IndexExtensions
{
    public static string CleanName(string? name)
    {
        if (name == null) { return Constants.DefaultIndex; }

        name = name.Trim();
        return string.IsNullOrEmpty(name) ? Constants.DefaultIndex : name;
    }
}
