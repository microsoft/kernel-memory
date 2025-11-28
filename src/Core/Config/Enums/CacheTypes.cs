// Copyright (c) Microsoft. All rights reserved.
using System.Text.Json.Serialization;

namespace KernelMemory.Core.Config.Enums;

/// <summary>
/// Type of cache storage backend
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CacheTypes
{
    /// <summary>SQLite database cache</summary>
    Sqlite,

    /// <summary>PostgreSQL database cache</summary>
    Postgres
}
