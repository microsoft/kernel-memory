// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.KernelMemory.MemoryDb.Qdrant.Client;

internal static class Verify
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void That(bool value, string message)
    {
        if (!value)
        {
            throw new ArgumentException(message);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void NotNull([NotNull] object? obj, string message)
    {
        if (obj != null) { return; }

        throw new ArgumentNullException(null, message);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void NotNullOrEmpty([NotNull] string? str, string message)
    {
        NotNull(str, message);
        if (!string.IsNullOrWhiteSpace(str)) { return; }

        throw new ArgumentOutOfRangeException(message);
    }

    internal static void NotNullOrEmpty<T>(IList<T> list, string message)
    {
        if (list == null || list.Count == 0)
        {
            throw new ArgumentOutOfRangeException(message);
        }
    }
}
