// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Microsoft.KernelMemory.MemoryDb.Elasticsearch.Internals;

/// <summary>
/// An Elasticsearch tag.
/// </summary>
public class ElasticsearchTag
{
    public const string NameField = "name";

    public const string ValueField = "value";

    /// <summary>
    /// Instantiates a new instance of <see cref="ElasticsearchTag"/>.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public ElasticsearchTag(string name, string? value = default)
    {
        ArgumentNullExceptionEx.ThrowIfNullOrWhiteSpace(name, nameof(name), "The tag name is NULL");

        this.Name = name;
        this.Value = value;
    }

    /// <summary>
    /// The name of this tag.
    /// </summary>
    [JsonPropertyName(NameField)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The value of this tag.
    /// </summary>
    [JsonPropertyName(ValueField)]
    public string? Value { get; set; }

    /// <inheritedDoc />
    public override string ToString()
    {
        return $"{this.Name}={this.Value}";
    }
}
