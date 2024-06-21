﻿// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Microsoft.KernelMemory.Context;

public interface IContext
{
    IDictionary<string, object?> Arguments { get; set; }
}

public static class ContextExtensions
{
    public static IContext? InitArgs(this IContext? context, IDictionary<string, object?> args)
    {
        if (context == null) { return null; }

        context.Arguments = new Dictionary<string, object?>();
        return context.SetArgs(args);
    }

    public static IContext? SetArgs(this IContext? context, IDictionary<string, object?> args)
    {
        if (context == null) { return null; }

        foreach (KeyValuePair<string, object?> arg in args)
        {
            context.Arguments[arg.Key] = arg.Value;
        }

        return context;
    }

    public static IContext? SetArg(this IContext? context, string key, object? value)
    {
        if (context == null) { return null; }

        if (context.Arguments == null!)
        {
            context.Arguments = new Dictionary<string, object?>();
        }

        context.Arguments[key] = value;
        return context;
    }

    public static IContext? ResetArgs(this IContext? context)
    {
        if (context == null) { return null; }

        context.Arguments = new Dictionary<string, object?>();
        return context;
    }

    public static bool TryGetArg<T>(this IContext? context, string key, [NotNullWhen(true)] out T? value)
    {
        if (context != null && context.Arguments.TryGetValue(key, out object? x))
        {
            if (x is JsonValue or JsonElement)
            {
                value = JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(x));
            }
            else
            {
                value = (T?)x;
            }

            return value != null;
        }

        value = default;
        return false;
    }

    public static bool TryGetArg(this IContext? context, string key, [NotNullWhen(true)] out object? value)
    {
        if (context != null && context.Arguments.TryGetValue(key, out object? x))
        {
            value = x;
            return value != null;
        }

        value = null;
        return false;
    }
}

public static class CustomContextExtensions
{
    public static string GetCustomEmptyAnswerTextOrDefault(this IContext? context, string defaultValue)
    {
        if (context.TryGetArg<string>(Constants.CustomContext.Rag.EmptyAnswer, out var customValue))
        {
            return customValue;
        }

        return defaultValue;
    }

    public static string GetCustomRagFactTemplateOrDefault(this IContext? context, string defaultValue)
    {
        if (context.TryGetArg<string>(Constants.CustomContext.Rag.FactTemplate, out var customValue))
        {
            return customValue;
        }

        return defaultValue;
    }

    public static string GetCustomRagPromptOrDefault(this IContext? context, string defaultValue)
    {
        if (context.TryGetArg<string>(Constants.CustomContext.Rag.Prompt, out var customValue))
        {
            return customValue;
        }

        return defaultValue;
    }

    public static int GetCustomRagMaxTokensOrDefault(this IContext? context, int defaultValue)
    {
        if (context.TryGetArg<int>(Constants.CustomContext.Rag.MaxTokens, out var customValue))
        {
            return customValue;
        }

        return defaultValue;
    }

    public static double GetCustomRagTemperatureOrDefault(this IContext? context, double defaultValue)
    {
        if (context.TryGetArg<int>(Constants.CustomContext.Rag.Temperature, out var customValue))
        {
            return customValue;
        }

        return defaultValue;
    }

    public static double GetCustomRagNucleusSamplingOrDefault(this IContext? context, double defaultValue)
    {
        if (context.TryGetArg<int>(Constants.CustomContext.Rag.NucleusSampling, out var customValue))
        {
            return customValue;
        }

        return defaultValue;
    }

    public static string GetCustomSummaryPromptOrDefault(this IContext? context, string defaultValue)
    {
        if (context.TryGetArg<string>(Constants.CustomContext.Summary.Prompt, out var customValue))
        {
            return customValue;
        }

        return defaultValue;
    }

    public static int GetCustomSummaryTargetTokenSizeOrDefault(this IContext? context, int defaultValue)
    {
        if (context.TryGetArg<int>(Constants.CustomContext.Summary.TargetTokenSize, out var customValue))
        {
            return customValue;
        }

        return defaultValue;
    }

    public static int GetCustomSummaryOverlappingTokensOrDefault(this IContext? context, int defaultValue)
    {
        if (context.TryGetArg<int>(Constants.CustomContext.Summary.OverlappingTokens, out var customValue))
        {
            return customValue;
        }

        return defaultValue;
    }

    public static int GetCustomPartitioningMaxTokensPerParagraphOrDefault(this IContext? context, int defaultValue)
    {
        if (context.TryGetArg<int>(Constants.CustomContext.Partitioning.MaxTokensPerParagraph, out var customValue))
        {
            return customValue;
        }

        return defaultValue;
    }

    public static int GetCustomPartitioningOverlappingTokensOrDefault(this IContext? context, int defaultValue)
    {
        if (context.TryGetArg<int>(Constants.CustomContext.Partitioning.OverlappingTokens, out var customValue))
        {
            return customValue;
        }

        return defaultValue;
    }

    public static int GetCustomEmbeddingGenerationBatchSizeOrDefault(this IContext? context, int defaultValue)
    {
        if (context.TryGetArg<int>(Constants.CustomContext.EmbeddingGeneration.BatchSize, out var customValue))
        {
            return customValue;
        }

        return defaultValue;
    }
}
