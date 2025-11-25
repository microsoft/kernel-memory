// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;

namespace Microsoft.KernelMemory.InteractiveSetup.UI;

internal static class DictionaryExtensions
{
    public static string TryGet(this Dictionary<string, object> data, string key)
    {
        return data.TryGetValue(key, out object? value) ? value.ToString() ?? string.Empty : string.Empty;
    }

    public static string TryGetOr(this Dictionary<string, object> data, string key, string fallbackValue)
    {
        return data.TryGetValue(key, out object? value) ? value.ToString() ?? string.Empty : fallbackValue;
    }
}
