// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Collections.Specialized;
using Microsoft.SemanticKernel.AI.Embeddings;

namespace Microsoft.SemanticMemory.Core.MemoryStorage;

public class MemoryRecord
{
    /// <summary>
    /// Unique record ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Embedding vector
    /// </summary>
    public Embedding<float> Vector { get; set; } = new();

    /// <summary>
    /// User ID
    /// </summary>
    public string Owner { get; set; } = string.Empty;

    /// <summary>
    /// Resource used to generate the embedding e.g. "pipeline ID + file name"
    /// Required to update/remove memories.
    /// </summary>
    public string SourceId { get; set; } = string.Empty;

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
    public NameValueCollection Tags { get; set; } = new();

    /// <summary>
    /// Optional Non-Searchable metadata processed client side.
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
    public Dictionary<string, object> Metadata { get; set; } = new();
}
