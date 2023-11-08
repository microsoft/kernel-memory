// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.Plugin;
using Microsoft.SemanticKernel;

// ReSharper disable once CheckNamespace
namespace Microsoft.KernelMemory;

// ReSharper disable ArrangeAttributes
public class MemoryPlugin
{
    /// <summary>
    /// Name of the input variable used to specify which memory index to use.
    /// </summary>
    public const string IndexParam = "index";

    /// <summary>
    /// Name of the input variable used to specify a unique id associated with stored information.
    ///
    /// Important: the text is stored in memory over multiple records, using an internal format,
    /// and Document ID is used across all the internal memory records generated. Each of these internal
    /// records has an internal ID that is not exposed to memory clients. Document ID can be used
    /// to ask questions about a specific text, to overwrite (update) the text, and to delete it.
    /// </summary>
    public const string DocumentIdParam = "documentId";

    /// <summary>
    /// Name of the input variable used to specify optional tags associated with stored information.
    ///
    /// Tags can be used to filter memories over one or multiple keys, e.g. userID, tenant, groups,
    /// project ID, room number, content type, year, region, etc.
    /// Each tag can have multiple values, e.g. to link a memory to multiple users.
    /// </summary>
    public const string TagsParam = "tags";

    /// <summary>
    /// Name of the input variable used to specify custom memory ingestion steps.
    /// The list is usually: "extract", "partition", "gen_embeddings", "save_embeddings"
    /// </summary>
    public const string StepsParam = "steps";

    /// <summary>
    /// Default document ID. When null, a new value is generated every time some information
    /// is saved into memory.
    /// </summary>
    private const string? DefaultDocumentId = null;

    /// <summary>
    /// Default index where to store and retrieve memory from. When null the service
    /// will use a default index for all information.
    /// </summary>
    private readonly string? _defaultIndex = null;

    /// <summary>
    /// Default collection of tags to add to information when ingesting.
    /// </summary>
    private readonly TagCollection? _defaultIngestionTags = null;

    /// <summary>
    /// Default collection of tags required when retrieving memory (using filters).
    /// </summary>
    private readonly TagCollection? _defaultRetrievalTags = null;

    /// <summary>
    /// Default ingestion steps when storing new memories.
    /// </summary>
    private readonly List<string>? _defaultIngestionSteps = null;

    /// <summary>
    /// Client to memory read/write. This is usually an instance of MemoryWebClient
    /// but the plugin allows to inject any IKernelMemory, e.g. in case of custom
    /// implementations and the embedded Serverless client.
    /// </summary>
    private readonly IKernelMemory _memory;

    public MemoryPlugin(
        Uri endpoint,
        string apiKey = "",
        string defaultIndex = "",
        TagCollection? defaultIngestionTags = null,
        TagCollection? defaultRetrievalTags = null,
        List<string>? defaultIngestionSteps = null)
        : this(
            new MemoryWebClient(endpoint.AbsoluteUri, apiKey),
            defaultIndex,
            defaultIngestionTags,
            defaultRetrievalTags,
            defaultIngestionSteps)
    {
    }

    public MemoryPlugin(
        IKernelMemory memoryClient,
        string defaultIndex = "",
        TagCollection? defaultIngestionTags = null,
        TagCollection? defaultRetrievalTags = null,
        List<string>? defaultIngestionSteps = null)
    {
        this._memory = memoryClient;
        this._defaultIndex = defaultIndex;
        this._defaultIngestionTags = defaultIngestionTags;
        this._defaultRetrievalTags = defaultRetrievalTags;
        this._defaultIngestionSteps = defaultIngestionSteps;
    }

    public MemoryPlugin(string serviceUrl, string apiKey = "")
        : this(new Uri(serviceUrl), apiKey)
    {
    }

    /// <summary>
    /// Store text information in long term memory
    /// </summary>
    /// <example>
    /// SKContext.Variables["input"] = "the capital of France is Paris"
    /// {{memory.importText $input }}
    /// </example>
    /// <example>
    /// SKContext.Variables["input"] = "the capital of France is Paris"
    /// SKContext.Variables[MemoryPlugin.IndexParam] = "geography"
    /// {{memory.importText $input }}
    /// </example>
    /// <example>
    /// SKContext.Variables["input"] = "the capital of France is Paris"
    /// SKContext.Variables[MemoryPlugin.DocumentIdParam] = "france001"
    /// {{memory.importText $input }}
    /// </example>
    /// <returns>Document ID</returns>
    [SKFunction, Description("Store text information in long term memory")]
    public async Task<string> ImportTextAsync(
        [Description("The information to save")]
        string input,
        [SKName(DocumentIdParam), Description("The document ID associated with the information to save"), DefaultValue(null)]
        string? documentId = null,
        [SKName(IndexParam), Description("Memories index associated with the information to save"), DefaultValue(null)]
        string? index = null,
        [SKName(TagsParam), Description("Memories index associated with the information to save"), DefaultValue(null)]
        TagCollectionWrapper? tags = null,
        [SKName(StepsParam), Description("Steps to parse the information and store in memory"), DefaultValue(null)]
        ListOfStringsWrapper? steps = null,
        ILoggerFactory? loggerFactory = null,
        CancellationToken cancellationToken = default)
    {
        return await this._memory.ImportTextAsync(
                text: input,
                documentId: documentId,
                index: index ?? this._defaultIndex,
                tags: tags ?? this._defaultIngestionTags,
                steps: steps ?? this._defaultIngestionSteps,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    [SKFunction, Description("Use long term memory to answer a quesion")]
    public async Task<string> AskAsync(
        [Description("The question to answer")]
        string input)
    {
        MemoryAnswer? answer = await this._memory.AskAsync(question: input).ConfigureAwait(false);
        return answer?.Result ?? string.Empty;
    }
}
