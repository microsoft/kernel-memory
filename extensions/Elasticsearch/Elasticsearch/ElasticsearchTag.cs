// Copyright (c) Free Mind Labs, Inc. All rights reserved.

using System.Text.Json.Serialization;

namespace Microsoft.KernelMemory.Elasticsearch;

/// <summary>
/// An elasticsearch tag.
/// </summary>
public class ElasticsearchTag
{
    /// <inheritdoc/>
    public const string NameField = "name";

    /// <inheritdoc/>
    public const string ValueField = "value";

    /// <summary>
    /// Instantiates a new instance of <see cref="ElasticsearchTag"/>.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public ElasticsearchTag(string name, string? value = default)
    {
        this.Name = name ?? throw new ArgumentNullException(nameof(name));
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
