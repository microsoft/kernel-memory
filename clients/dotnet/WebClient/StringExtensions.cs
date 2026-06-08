// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.KernelMemory;

internal static class StringExtensions
{
    public static string CleanBaseAddress(this string endpoint)
    {
        ArgumentNullExceptionEx.ThrowIfNull(endpoint, nameof(endpoint), "Kernel Memory API endpoint is NULL");

        return endpoint.TrimEnd('/') + '/';
    }

    public static string CleanUrlPath(this string path)
    {
        if (string.IsNullOrWhiteSpace(path)) { path = "/"; }

        return path.TrimStart('/');
    }
}
