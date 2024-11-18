// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Net;

namespace Microsoft.KernelMemory.Diagnostics;

public static class HttpErrors
{
    // Errors that might disappear by retrying
    private static readonly HashSet<int> s_transientErrors =
    [
        (int)HttpStatusCode.InternalServerError,
        (int)HttpStatusCode.BadGateway,
        (int)HttpStatusCode.ServiceUnavailable,
        (int)HttpStatusCode.GatewayTimeout,
        (int)HttpStatusCode.InsufficientStorage
    ];

    public static bool IsTransientError(this HttpStatusCode statusCode)
    {
        return s_transientErrors.Contains((int)statusCode);
    }

    public static bool IsTransientError(this HttpStatusCode? statusCode)
    {
        return statusCode.HasValue && s_transientErrors.Contains((int)statusCode.Value);
    }

    public static bool IsTransientError(int statusCode)
    {
        return s_transientErrors.Contains(statusCode);
    }

    public static bool IsFatalError(this HttpStatusCode statusCode)
    {
        return IsError(statusCode) && !IsTransientError(statusCode);
    }

    public static bool IsFatalError(this HttpStatusCode? statusCode)
    {
        return statusCode.HasValue && IsError(statusCode) && !IsTransientError(statusCode);
    }

    public static bool IsFatalError(int statusCode)
    {
        return IsError(statusCode) && !IsTransientError(statusCode);
    }

    private static bool IsError(this HttpStatusCode? statusCode)
    {
        return statusCode.HasValue && (int)statusCode.Value >= 400;
    }

    private static bool IsError(int statusCode)
    {
        return statusCode >= 400;
    }
}
