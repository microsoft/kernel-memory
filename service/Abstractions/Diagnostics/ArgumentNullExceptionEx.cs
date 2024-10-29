// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

#pragma warning disable CS8777
public static class ArgumentNullExceptionEx
{
    public static void ThrowIfNull([NotNull] object? argument, string? paramName = null, string message = "")
    {
        if (argument != null) { return; }

        throw new ArgumentNullException(paramName, message);
    }

    public static void ThrowIfNullOrWhiteSpace([NotNull] string? argument, string? paramName = null, string message = "")
    {
        if (!string.IsNullOrWhiteSpace(argument)) { return; }

        throw new ArgumentNullException(paramName, message);
    }

    public static void ThrowIfNullOrEmpty([NotNull] string? argument, string? paramName = null, string message = "")
    {
        if (!string.IsNullOrEmpty(argument)) { return; }

        throw new ArgumentNullException(paramName, message);
    }

    public static void ThrowIfEmpty<T>([NotNull] IList<T>? argument, string? paramName = null, string message = "")
    {
        if (argument is { Count: > 0 }) { return; }

        throw new ArgumentNullException(paramName, message);
    }
}
