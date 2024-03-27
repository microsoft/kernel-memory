// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.KernelMemory.MemoryDb.Qdrant.Client.Http;

internal sealed class Filter
{
    internal sealed class OrClause
    {
        [JsonPropertyName("should")]
        public List<object> Clauses { get; set; }

        internal OrClause()
        {
            this.Clauses = new();
        }

        internal OrClause Or(object condition)
        {
            this.Clauses.Add(condition);
            return this;
        }

        internal OrClause OrValue(string key, object value)
        {
            return this.Or(new MatchValueClause(key, value));
        }

        internal void Validate()
        {
            Verify.NotNull(this.Clauses, "Filter clauses are NULL");
            foreach (var x in this.Clauses)
            {
                switch (x)
                {
                    case AndClause ac:
                        ac.Validate();
                        break;

                    case OrClause oc:
                        oc.Validate();
                        break;

                    case MatchValueClause mvc:
                        mvc.Validate();
                        break;
                }
            }
        }
    }

    internal sealed class AndClause
    {
        [JsonPropertyName("must")]
        public List<object> Clauses { get; set; }

        internal AndClause()
        {
            this.Clauses = new();
        }

        internal AndClause And(object condition)
        {
            this.Clauses.Add(condition);
            return this;
        }

        internal AndClause AndValue(string key, object value)
        {
            return this.And(new MatchValueClause(key, value));
        }

        internal void Validate()
        {
            Verify.NotNull(this.Clauses, "Filter clauses are NULL");
            foreach (var x in this.Clauses)
            {
                switch (x)
                {
                    case AndClause ac:
                        ac.Validate();
                        break;

                    case OrClause oc:
                        oc.Validate();
                        break;

                    case MatchValueClause mvc:
                        mvc.Validate();
                        break;
                }
            }
        }
    }

    internal sealed class MatchValueClause
    {
        [JsonPropertyName("key")]
        public string Key { get; set; }

        [JsonPropertyName("match")]
        public MatchValue Match { get; set; }

        public MatchValueClause()
        {
            this.Match = new();
            this.Key = string.Empty;
        }

        public MatchValueClause(string key, object value) : this()
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

    internal sealed class MatchValue
    {
        [JsonPropertyName("value")]
        public object Value { get; set; }

        public MatchValue()
        {
            this.Value = string.Empty;
        }
    }
}
