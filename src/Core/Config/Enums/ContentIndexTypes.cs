// Copyright (c) Microsoft. All rights reserved.
using System.Text.Json.Serialization;

namespace KernelMemory.Core.Config.Enums;

/// <summary>
/// Type of Entity Framework backed content index
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ContentIndexTypes
{
    /// <summary>SQLite database for local/single-user scenarios</summary>
    Sqlite,

    /// <summary>PostgreSQL database for multi-user/production scenarios</summary>
    Postgres
}
