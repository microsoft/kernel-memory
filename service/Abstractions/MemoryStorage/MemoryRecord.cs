// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.KernelMemory.MemoryStorage;

public class MemoryRecord
{
    // Memory Db Record schema versioning - Introduced after version 0.23.231218.1
    private const string SchemaVersionZero = "";
    private const string SchemaVersion20231218A = "20231218A";
    private const string CurrentSchemaVersion = SchemaVersion20231218A;

    // Internal data
    private TagCollection _tags = new();
    private Dictionary<string, object> _payload = new();

    /// <summary>
    /// Unique record ID
    /// </summary>
    [JsonPropertyName("id")]
    [JsonPropertyOrder(1)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Embedding vector
    /// </summary>
    [JsonPropertyName("vector")]
    [JsonPropertyOrder(100)]
    [JsonConverter(typeof(Embedding.JsonConverter))]
    public Embedding Vector { get; set; } = new();

    /// <summary>
    /// Optional Searchable Key=Value tags (string => string[] collection)
    ///
    /// Multiple values per keys are supported.
    /// e.g. [ "Collection=Work", "Project=1", "Project=2", "Project=3", "Type=Chat", "LLM=AzureAda2" ]
    ///
    /// Use cases:
    ///  * collections, e.g. [ "Collection=Project1", "Collection=Work" ]
    ///  * folders, e.g. [ "Folder=Inbox", "Folder=Spam" ]
    ///  * content types, e.g. [ "Type=Chat" ]
    ///  * versioning, e.g. [ "LLM=AzureAda2", "Schema=1.0" ]
    ///  * etc.
    /// </summary>
    [JsonPropertyName("tags")]
    [JsonPropertyOrder(2)]
    public TagCollection Tags
    {
        get
        {
            if (this.UpgradeRequired()) { this.Upgrade(); }

            return this._tags;
        }
        set
        {
            this._tags = value;
        }
    }

    /// <summary>
    /// Optional Non-Searchable payload processed client side.
    ///
    /// Use cases:
    ///  * citations
    ///  * original text
    ///  * descriptions
    ///  * embedding generator name
    ///  * URLs
    ///  * content type
    ///  * timestamps
    ///  * etc.
    /// </summary>
    [JsonPropertyName("payload")]
    [JsonPropertyOrder(3)]
    public Dictionary<string, object> Payload
    {
        get
        {
            if (this.UpgradeRequired()) { this.Upgrade(); }

            return this._payload;
        }
        set
        {
            this._payload = value;
        }
    }

    /// <summary>
    /// Check if the current state requires an upgrade
    /// </summary>
    private bool UpgradeRequired()
    {
        if (this._payload == null) { return true; }

        if (!this._payload.TryGetValue(Constants.ReservedPayloadSchemaVersionField, out object? versionValue))
        {
            return true;
        }

        return (versionValue == null || versionValue.ToString() != CurrentSchemaVersion);
    }

#pragma warning disable CA1820 // readability
    /// <summary>
    /// Upgrade the record to the latest schema
    /// </summary>
    private void Upgrade()
    {
        if (this._payload == null) { this._payload = new(); }

        if (this._tags == null) { this._tags = new(); }

        string version = SchemaVersionZero;
        if (this._payload.TryGetValue(Constants.ReservedPayloadSchemaVersionField, out object? versionValue))
        {
            version = versionValue == null ? string.Empty : versionValue.ToString()!;
        }

        // Upgrade to "20231218A"
        if (version == SchemaVersionZero)
        {
            if (!this._payload.ContainsKey(Constants.ReservedPayloadUrlField))
            {
                this._payload[Constants.ReservedPayloadUrlField] = string.Empty;
            }

            version = SchemaVersion20231218A;
            this._payload[Constants.ReservedPayloadSchemaVersionField] = SchemaVersion20231218A;
        }

        // if (version == SchemaVersion20231218A)
        // {
        //     Nothing to do, this is the latest version
        //     Add future upgrade logic here if required
        // }

        this._payload[Constants.ReservedPayloadSchemaVersionField] = CurrentSchemaVersion;
    }
#pragma warning restore CA1820
}
