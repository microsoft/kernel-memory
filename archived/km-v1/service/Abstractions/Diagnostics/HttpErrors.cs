// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Net;

namespace Microsoft.KernelMemory.Diagnostics;

public static class HttpErrors
{
    // Errors that might disappear by retrying
    private static readonly HashSet<int> s_transientErrors =
    [
        (int)HttpStatusCode.RequestTimeout, // 408
        (int)HttpStatusCode.PreconditionFailed, // 412
        (int)HttpStatusCode.Locked, // 423
        (int)HttpStatusCode.TooManyRequests, // 429
        (int)HttpStatusCode.InternalServerError, // 500
        (int)HttpStatusCode.BadGateway, // 502
        (int)HttpStatusCode.ServiceUnavailable, // 503
        (int)HttpStatusCode.GatewayTimeout, // 504
        (int)HttpStatusCode.InsufficientStorage // 507
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
