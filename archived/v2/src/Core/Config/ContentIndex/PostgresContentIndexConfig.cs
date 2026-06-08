// Copyright (c) Microsoft. All rights reserved.
using System.Text.Json.Serialization;
using KernelMemory.Core.Config.Enums;
using KernelMemory.Core.Config.Validation;

namespace KernelMemory.Core.Config.ContentIndex;

/// <summary>
/// PostgreSQL content index configuration
/// </summary>
public sealed class PostgresContentIndexConfig : ContentIndexConfig
{
    /// <inheritdoc />
    [JsonIgnore]
    public override ContentIndexTypes Type => ContentIndexTypes.Postgres;

    /// <summary>
    /// PostgreSQL connection string
    /// Can be overridden by Aspire environment variables
    /// </summary>
    [JsonPropertyName("connectionString")]
    public string ConnectionString { get; set; } = string.Empty;

    /// <inheritdoc />
    public override void Validate(string path)
    {
        if (string.IsNullOrWhiteSpace(this.ConnectionString))
        {
            throw new ConfigException($"{path}.ConnectionString",
                "PostgreSQL connection string is required");
        }
    }
}
