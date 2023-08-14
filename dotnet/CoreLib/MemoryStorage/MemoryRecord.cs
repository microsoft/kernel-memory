// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticMemory.Client.Models;

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
    public TagCollection Tags { get; set; } = new();

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
    public Dictionary<string, object> Payload { get; set; } = new();
}
