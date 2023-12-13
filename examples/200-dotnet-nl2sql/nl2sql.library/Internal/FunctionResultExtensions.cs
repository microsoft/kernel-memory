// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.SemanticKernel;

namespace SemanticKernel.Data.Nl2Sql.Library.Internal;

internal static class FunctionResultExtensions
{
    private static readonly HashSet<char> s_delimetersResult = new() { '\'', '"', '`' };

    public static string ParseValue(this FunctionResult result, string? label = null)
    {
        if (result == null)
        {
            return string.Empty;
        }

        var resultText = TrimDelimeters(result.GetValue<string>() ?? string.Empty);

        if (!string.IsNullOrWhiteSpace(label))
        {
            // Trim out label, if present.
            int index = resultText.IndexOf($"{label}", StringComparison.OrdinalIgnoreCase);
            if (index == 0)
            {
                resultText = resultText.Substring(index + label.Length + 1).Trim();
            }
        }

        return resultText;
    }

    private static string TrimDelimeters(string expression)
    {
        for (var index = 0; index < expression.Length; ++index)
        {
            if (s_delimetersResult.Contains(expression[index]) &&
                s_delimetersResult.Contains(expression[expression.Length - index - 1]))
            {
                continue;
            }

            return expression.Substring(index, expression.Length - (index * 2)).Trim();
        }

        return expression;
    }
}
