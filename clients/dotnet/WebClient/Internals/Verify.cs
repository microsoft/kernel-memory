// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.KernelMemory.Internals;

internal static class Verify
{
    public static void ValidateUrl(
        string url,
        bool requireHttps,
        bool allowReservedIp,
        bool allowQuery)
    {
        static bool IsReservedIpAddress(string host)
        {
            return host.StartsWith("0.", StringComparison.Ordinal) ||
                   host.StartsWith("10.", StringComparison.Ordinal) ||
                   host.StartsWith("127.", StringComparison.Ordinal) ||
                   host.StartsWith("169.254.", StringComparison.Ordinal) ||
                   host.StartsWith("192.0.0.", StringComparison.Ordinal) ||
                   host.StartsWith("192.88.99.", StringComparison.Ordinal) ||
                   host.StartsWith("192.168.", StringComparison.Ordinal) ||
                   host.StartsWith("255.255.255.255", StringComparison.Ordinal);
        }

        ArgumentNullExceptionEx.ThrowIfNullOrEmpty(url, nameof(url), "The URL is empty");

        if (requireHttps && url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"The URL `{url}` is not safe, it must start with https://");
        }

        if (requireHttps && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"The URL `{url}` is incomplete, enter a valid URL starting with 'https://");
        }

        bool result = Uri.TryCreate(url, UriKind.Absolute, out Uri? uri);
        if (!result || string.IsNullOrEmpty(uri?.Host))
        {
            throw new ArgumentException($"The URL `{url}` is not valid");
        }

        if (requireHttps && uri!.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException($"The URL `{url}` is not safe, it must start with https://");
        }

        if (!allowReservedIp && (uri!.IsLoopback || IsReservedIpAddress(uri.Host)))
        {
            throw new ArgumentException($"The URL `{url}` is not safe, it cannot point to a reserved network address");
        }

        if (!allowQuery && !string.IsNullOrEmpty(uri!.Query))
        {
            throw new ArgumentException($"The URL `{url}` is not valid, it cannot contain query parameters");
        }

        if (!string.IsNullOrEmpty(uri!.Fragment))
        {
            throw new ArgumentException($"The URL `{url}` is not valid, it cannot contain URL fragments");
        }
    }
}
