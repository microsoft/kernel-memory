// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.KernelMemory.Diagnostics;

public static class CodeQL
{
    /// <summary>
    /// See https://codeql.github.com/codeql-query-help/csharp/cs-log-forging/
    /// </summary>
    public static string? NLF(this string? text)
    {
        if (text == null) { return text; }

        return text
            .Replace("\n", "[char(10)]", StringComparison.Ordinal)
            .Replace("\r", "[char(13)]", StringComparison.Ordinal);
    }
}
