// Copyright (c) Microsoft. All rights reserved.

using System;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

public static class ArgumentExceptionEx
{
    public static void ThrowIf(bool condition, string? paramName, string message)
    {
        if (!condition) { return; }

        throw new ArgumentException(paramName, message);
    }

    public static void ThrowIfNot(bool condition, string? paramName, string message)
    {
        if (condition) { return; }

        throw new ArgumentException(paramName, message);
    }
}
