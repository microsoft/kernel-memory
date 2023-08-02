// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Logging;
using Microsoft.SemanticMemory.Core.Configuration;

namespace Microsoft.SemanticMemory.Core.Diagnostics;

/// <summary>
/// Create and cache a logger instance using the same
/// configuration sources supported by Semantic Memory config.
/// </summary>
/// <typeparam name="T"></typeparam>
public static class DefaultLogger<T>
{
    public static readonly ILogger<T> Instance = SemanticMemoryConfig
        .GetLogFactory()
        .CreateLogger<T>();
}
