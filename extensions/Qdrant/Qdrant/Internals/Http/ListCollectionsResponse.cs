// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.KernelMemory.MemoryDb.Qdrant.Client.Http;

internal sealed class ListCollectionsResponse : QdrantResponse
{
    internal sealed class CollectionResult
    {
        internal sealed class CollectionDescription
        {
            /// <summary>
            /// The name of a collection
            /// </summary>
            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;
        }

        /// <summary>
        /// List of the collection names that the qdrant database contains.
        /// </summary>
        [JsonPropertyName("collections")]
        public IList<CollectionDescription> Collections { get; set; } = [];
    }

    /// <summary>
    /// Result containing a list of collection names
    /// </summary>
    [JsonPropertyName("result")]
    public CollectionResult? Result { get; set; }
}
