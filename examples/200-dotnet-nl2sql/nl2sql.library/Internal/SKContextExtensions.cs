// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.SemanticKernel.Orchestration;

namespace SemanticKernel.Data.Nl2Sql.Library.Internal;

internal static class SKContextExtensions
{
    public static string GetResult(this SKContext context, string? label = null)
    {
        if (context == null)
        {
            return string.Empty;
        }

        var result = context.Result;

        if (!string.IsNullOrWhiteSpace(label))
        {
            // Trim out label, if present.
            int index = result.IndexOf($"{label}:", StringComparison.OrdinalIgnoreCase);
            if (index > -1)
            {
                result = result.Substring(index + label.Length + 1);
            }
        }

        return result;
    }
}
