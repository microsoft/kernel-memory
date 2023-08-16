// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.SemanticMemory.Core.MemoryStorage.Qdrant.Client.Http;

internal sealed class Filter
{
    internal sealed class Match
    {
        [JsonPropertyName("value")]
        public object Value { get; set; }

        public Match()
        {
            this.Value = string.Empty;
        }
    }

    internal sealed class Must
    {
        [JsonPropertyName("key")]
        public string Key { get; set; }

        [JsonPropertyName("match")]
        public Match Match { get; set; }

        public Must()
        {
            this.Match = new();
            this.Key = string.Empty;
        }

        public Must(string key, object value) : this()
        {
            this.Key = key;
            this.Match.Value = value;
        }

        internal void Validate()
        {
            Verify.NotNull(this.Key, "The filter key is NULL");
            Verify.NotNull(this.Match, "The filter match is NULL");
        }
    }

    [JsonPropertyName("must")]
    public List<Must> Conditions { get; set; }

    internal Filter()
    {
        this.Conditions = new();
    }

    internal Filter ValueMustMatch(string key, object value)
    {
        this.Conditions.Add(new Must(key, value));
        return this;
    }

    internal void Validate()
    {
        Verify.NotNull(this.Conditions, "Filter conditions are NULL");
        foreach (var x in this.Conditions)
        {
            x.Validate();
        }
    }
}
