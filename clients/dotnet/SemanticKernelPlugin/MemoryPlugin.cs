// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.SemanticKernelPlugin.Internals;
using Microsoft.SemanticKernel;

namespace Microsoft.KernelMemory;

/// <summary>
/// Kernel Memory Plugin
///
/// Recommended name: "memory"
///
/// Functions:
/// * memory.save
/// * memory.saveFile
/// * memory.saveWebPage
/// * memory.ask
/// * memory.search
/// * memory.delete
///
/// </summary>
public class MemoryPlugin
{
    /// <summary>
    /// Name of the input variable used to specify which memory index to use.
    /// </summary>
    public const string IndexParam = "index";

    /// <summary>
    /// Name of the input variable used to specify a file path.
    /// </summary>
    public const string FilePathParam = "filePath";

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
    /// Name of the input variable used to specify a web URL.
    /// </summary>
    public const string UrlParam = "url";

    /// <summary>
    /// Name of the input variable used to specify a search query.
    /// </summary>
    public const string QueryParam = "query";

    /// <summary>
    /// Name of the input variable used to specify a question to answer.
    /// </summary>
    public const string QuestionParam = "question";

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
    /// The list is usually: "extract", "partition", "gen_embeddings", "save_records"
    /// </summary>
    public const string StepsParam = "steps";

    /// <summary>
    /// Name of the input variable used to specify custom minimum relevance for the memories to retrieve.
    /// </summary>
    public const string MinRelevanceParam = "minRelevance";

    /// <summary>
    /// Name of the input variable used to specify the maximum number of items to return.
    /// </summary>
    public const string LimitParam = "limit";

    /// <summary>
    /// Default index where to store and retrieve memory from. When null the service
    /// will use a default index for all information.
    /// </summary>
    private readonly string? _defaultIndex;

    /// <summary>
    /// Default collection of tags to add to information when ingesting.
    /// </summary>
    private readonly TagCollection? _defaultIngestionTags;

    /// <summary>
    /// Default collection of tags required when retrieving memory (using filters).
    /// </summary>
    private readonly TagCollection? _defaultRetrievalTags;

    /// <summary>
    /// Default ingestion steps when storing new memories.
    /// </summary>
    private readonly List<string>? _defaultIngestionSteps;

    /// <summary>
    /// Whether to wait for the asynchronous ingestion to be complete when storing new memories.
    /// Note: the plugin will wait max <see cref="_maxIngestionWait"/> seconds to avoid blocking callers for too long.
    /// </summary>
    private readonly bool _waitForIngestionToComplete;

    /// <summary>
    /// Max time to wait for ingestion completion when <see cref="_waitForIngestionToComplete"/> is set to True.
    /// </summary>
    private readonly TimeSpan _maxIngestionWait = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Client to memory read/write. This is usually an instance of MemoryWebClient
    /// but the plugin allows to inject any IKernelMemory, e.g. in case of custom
    /// implementations and the embedded Serverless client.
    /// </summary>
    private readonly IKernelMemory _memory;

    /// <summary>
    /// Create new instance using MemoryWebClient pointed at the given endpoint.
    /// </summary>
    /// <param name="endpoint">Memory Service endpoint</param>
    /// <param name="apiKey">Memory Service authentication API Key</param>
    /// <param name="apiKeyHeader">Name of the HTTP header used to send the Memory API Key</param>
    /// <param name="defaultIndex">Default Memory Index to use when none is specified. Optional. Can be overridden on each call.</param>
    /// <param name="defaultIngestionTags">Default Tags to add to memories when importing data. Optional. Can be overridden on each call.</param>
    /// <param name="defaultRetrievalTags">Default Tags to require when searching memories. Optional. Can be overridden on each call.</param>
    /// <param name="defaultIngestionSteps">Pipeline steps to use when importing memories. Optional. Can be overridden on each call.</param>
    /// <param name="waitForIngestionToComplete">Whether to wait for the asynchronous ingestion to be complete when storing new memories.</param>
    public MemoryPlugin(
        Uri endpoint,
        string apiKey = "",
        string apiKeyHeader = "Authorization",
        string defaultIndex = "",
        TagCollection? defaultIngestionTags = null,
        TagCollection? defaultRetrievalTags = null,
        List<string>? defaultIngestionSteps = null,
        bool waitForIngestionToComplete = false)
        : this(
            new MemoryWebClient(endpoint.AbsoluteUri, apiKey: apiKey, apiKeyHeader: apiKeyHeader),
            defaultIndex,
            defaultIngestionTags,
            defaultRetrievalTags,
            defaultIngestionSteps,
            waitForIngestionToComplete)
    {
    }

    /// <summary>
    /// Create new instance using MemoryWebClient pointed at the given endpoint.
    /// </summary>
    /// <param name="serviceUrl">Memory Service endpoint</param>
    /// <param name="apiKey">Memory Service authentication API  Key</param>
    /// <param name="waitForIngestionToComplete">Whether to wait for the asynchronous ingestion to be complete when storing new memories.</param>
    public MemoryPlugin(
        string serviceUrl,
        string apiKey = "",
        bool waitForIngestionToComplete = false)
        : this(
            endpoint: new Uri(serviceUrl),
            apiKey: apiKey,
            waitForIngestionToComplete: waitForIngestionToComplete)
    {
    }

    /// <summary>
    /// Create a new instance using a custom IKernelMemory implementation.
    /// </summary>
    /// <param name="memoryClient">Custom IKernelMemory implementation</param>
    /// <param name="defaultIndex">Default Memory Index to use when none is specified. Optional. Can be overridden on each call.</param>
    /// <param name="defaultIngestionTags">Default Tags to add to memories when importing data. Optional. Can be overridden on each call.</param>
    /// <param name="defaultRetrievalTags">Default Tags to require when searching memories. Optional. Can be overridden on each call.</param>
    /// <param name="defaultIngestionSteps">Pipeline steps to use when importing memories. Optional. Can be overridden on each call.</param>
    /// <param name="waitForIngestionToComplete">Whether to wait for the asynchronous ingestion to be complete when storing new memories.</param>
    public MemoryPlugin(
        IKernelMemory memoryClient,
        string defaultIndex = "",
        TagCollection? defaultIngestionTags = null,
        TagCollection? defaultRetrievalTags = null,
        List<string>? defaultIngestionSteps = null,
        bool waitForIngestionToComplete = false)
    {
        this._memory = memoryClient;
        this._defaultIndex = defaultIndex;
        this._defaultIngestionTags = defaultIngestionTags;
        this._defaultRetrievalTags = defaultRetrievalTags;
        this._defaultIngestionSteps = defaultIngestionSteps;
        this._waitForIngestionToComplete = waitForIngestionToComplete;
    }

    /// <summary>
    /// Store text information in long term memory.
    ///
    /// Usage from prompts: '{{memory.save ...}}'
    /// </summary>
    /// <example>
    /// SKContext.Variables["input"] = "the capital of France is Paris"
    /// {{memory.save $input }}
    /// </example>
    /// <example>
    /// SKContext.Variables["input"] = "the capital of France is Paris"
    /// SKContext.Variables[MemoryPlugin.IndexParam] = "geography"
    /// {{memory.save $input }}
    /// </example>
    /// <example>
    /// SKContext.Variables["input"] = "the capital of France is Paris"
    /// SKContext.Variables[MemoryPlugin.DocumentIdParam] = "france001"
    /// {{memory.save $input }}
    /// </example>
    /// <returns>Document ID</returns>
    [KernelFunction, Description("Store in memory the given text")]
    public async Task<string> SaveAsync(
        [Description("The text to save in memory")]
        string input,
        [ /*SKName(DocumentIdParam),*/ Description("The document ID associated with the information to save"), DefaultValue(null)]
        string? documentId = null,
        [ /*SKName(IndexParam),*/ Description("Memories index associated with the information to save"), DefaultValue(null)]
        string? index = null,
        [ /*SKName(TagsParam),*/ Description("Memories index associated with the information to save"), DefaultValue(null)]
        TagCollectionWrapper? tags = null,
        [ /*SKName(StepsParam),*/ Description("Steps to parse the information and store in memory"), DefaultValue(null)]
        ListOfStringsWrapper? steps = null,
        ILoggerFactory? loggerFactory = null,
        CancellationToken cancellationToken = default)
    {
        string id = await this._memory.ImportTextAsync(
                text: input,
                documentId: documentId,
                index: index ?? this._defaultIndex,
                tags: tags ?? this._defaultIngestionTags,
                steps: steps ?? this._defaultIngestionSteps,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        await this.WaitForDocumentReadinessAsync(id, cancellationToken).ConfigureAwait(false);

        return id;
    }

    /// <summary>
    /// Store a file content in long term memory.
    ///
    /// Usage from prompts: '{{memory.saveFile ...}}'
    /// </summary>
    /// <example>
    /// SKContext.Variables["input"] = "C:\Documents\presentation.pptx"
    /// {{memory.saveFile $input }}
    /// </example>
    /// <example>
    /// SKContext.Variables["input"] = "C:\Documents\presentation.pptx"
    /// SKContext.Variables[MemoryPlugin.IndexParam] = "work"
    /// {{memory.saveFile $input }}
    /// </example>
    /// <example>
    /// SKContext.Variables["input"] = "C:\Documents\presentation.pptx"
    /// SKContext.Variables[MemoryPlugin.DocumentIdParam] = "presentation001"
    /// {{memory.saveFile $input }}
    /// </example>
    /// <returns>Document ID</returns>
    [KernelFunction, Description("Store in memory the information extracted from a file")]
    public async Task<string> SaveFileAsync(
        [ /*SKName(FilePathParam),*/ Description("Path of the file to save in memory")]
        string filePath,
        [ /*SKName(DocumentIdParam),*/ Description("The document ID associated with the information to save"), DefaultValue(null)]
        string? documentId = null,
        [ /*SKName(IndexParam),*/ Description("Memories index associated with the information to save"), DefaultValue(null)]
        string? index = null,
        [ /*SKName(TagsParam),*/ Description("Memories index associated with the information to save"), DefaultValue(null)]
        TagCollectionWrapper? tags = null,
        [ /*SKName(StepsParam),*/ Description("Steps to parse the information and store in memory"), DefaultValue(null)]
        ListOfStringsWrapper? steps = null,
        ILoggerFactory? loggerFactory = null,
        CancellationToken cancellationToken = default)
    {
        var id = await this._memory.ImportDocumentAsync(
                filePath: filePath,
                documentId: documentId,
                tags: tags ?? this._defaultIngestionTags,
                index: index ?? this._defaultIndex,
                steps: steps ?? this._defaultIngestionSteps,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        await this.WaitForDocumentReadinessAsync(id, cancellationToken).ConfigureAwait(false);

        return id;
    }

    /// <summary>
    /// Store in memory the information extracted from a web page
    /// </summary>
    /// <param name="url">Web page URL</param>
    /// <param name="documentId">The document ID associated with the information to save</param>
    /// <param name="index">Memories index containing the information to save</param>
    /// <param name="tags">Tas/Labels associated with the information to save</param>
    /// <param name="steps">Steps to parse the information and store in memory</param>
    /// <param name="loggerFactory">Logging factory</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    /// <returns>Document ID</returns>
    [KernelFunction, Description("Store in memory the information extracted from a web page")]
    public async Task<string> SaveWebPageAsync(
        [ /*SKName(UrlParam),*/ Description("Complete URL of the web page to save")]
        string url,
        [ /*SKName(DocumentIdParam),*/ Description("The document ID associated with the information to save"), DefaultValue(null)]
        string? documentId = null,
        [ /*SKName(IndexParam),*/ Description("Memories index containing the information to save"), DefaultValue(null)]
        string? index = null,
        [ /*SKName(TagsParam),*/ Description("Tas/Labels associated with the information to save"), DefaultValue(null)]
        TagCollectionWrapper? tags = null,
        [ /*SKName(StepsParam),*/ Description("Steps to parse the information and store in memory"), DefaultValue(null)]
        ListOfStringsWrapper? steps = null,
        ILoggerFactory? loggerFactory = null,
        CancellationToken cancellationToken = default)
    {
        var id = await this._memory.ImportWebPageAsync(
                url: url,
                documentId: documentId,
                tags: tags ?? this._defaultIngestionTags,
                index: index ?? this._defaultIndex,
                steps: steps ?? this._defaultIngestionSteps,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        await this.WaitForDocumentReadinessAsync(id, cancellationToken).ConfigureAwait(false);

        return id;
    }

    /// <summary>
    /// Return up to N memories related to the input text
    /// </summary>
    /// <param name="query">The text to search in memory</param>
    /// <param name="index">Memories index container to search for information</param>
    /// <param name="minRelevance">Minimum relevance of the memories to return</param>
    /// <param name="limit">Maximum number of memories to return</param>
    /// <param name="tags">Memories key-value tags to filter information</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    /// <returns>JSON string containing the list of memories</returns>
    [KernelFunction, Description("Return up to N memories related to the input text")]
    public async Task<string> SearchAsync(
        [ /*SKName(QueryParam),*/ Description("The text to search in memory")]
        string query,
        [ /*SKName(IndexParam),*/ Description("Memories index container to search for information"), DefaultValue("")]
        string? index = null,
        [ /*SKName(MinRelevanceParam),*/ Description("Minimum relevance of the memories to return"), DefaultValue(0d)]
        double minRelevance = 0,
        [ /*SKName(LimitParam),*/ Description("Maximum number of memories to return"), DefaultValue(1)]
        int limit = 1,
        [ /*SKName(TagsParam),*/ Description("Memories key-value tags to filter information"), DefaultValue(null)]
        TagCollectionWrapper? tags = null,
        CancellationToken cancellationToken = default)
    {
        SearchResult result = await this._memory
            .SearchAsync(
                query: query,
                index: index ?? this._defaultIndex,
                filter: TagsToMemoryFilter(tags ?? this._defaultRetrievalTags),
                minRelevance: minRelevance,
                limit: limit,
                cancellationToken: cancellationToken).ConfigureAwait(false);

        if (result.Results.Count == 0)
        {
            return string.Empty;
        }

        // Return the first chunk(s) of the relevant documents
        return limit == 1
            ? result.Results.First().Partitions.First().Text
            : JsonSerializer.Serialize(result.Results.Select(x => x.Partitions.First().Text));
    }

    /// <summary>
    /// Answer a question using the information stored in long term memory.
    ///
    /// Usage from prompts: '{{memory.ask ...}}'
    /// </summary>
    /// <returns>The answer returned by the memory.</returns>
    [KernelFunction, Description("Use long term memory to answer a question")]
    public async Task<string> AskAsync(
        [ /*SKName(QuestionParam),*/ Description("The question to answer")]
        string question,
        [ /*SKName(IndexParam),*/ Description("Memories index to search for answers"), DefaultValue("")]
        string? index = null,
        [ /*SKName(MinRelevanceParam),*/ Description("Minimum relevance of the sources to consider"), DefaultValue(0d)]
        double minRelevance = 0,
        [ /*SKName(TagsParam),*/ Description("Memories tags to search for information"), DefaultValue(null)]
        TagCollectionWrapper? tags = null,
        ILoggerFactory? loggerFactory = null,
        CancellationToken cancellationToken = default)
    {
        MemoryAnswer answer = await this._memory.AskAsync(
            question: question,
            index: index ?? this._defaultIndex,
            filter: TagsToMemoryFilter(tags ?? this._defaultRetrievalTags),
            minRelevance: minRelevance,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        return answer.Result;
    }

    /// <summary>
    /// Remove from memory all the information extracted from the given document ID
    ///
    /// Usage from prompts: '{{memory.delete ...}}'
    /// </summary>
    [KernelFunction, Description("Remove from memory all the information extracted from the given document ID")]
    public Task DeleteAsync(
        [ /*SKName(DocumentIdParam),*/ Description("The document to delete")]
        string documentId,
        [ /*SKName(IndexParam),*/ Description("Memories index where the document is stored"), DefaultValue("")]
        string? index = null,
        CancellationToken cancellationToken = default)
    {
        return this._memory.DeleteDocumentAsync(
            documentId: documentId,
            index: index ?? this._defaultIndex,
            cancellationToken: cancellationToken);
    }

    private async Task WaitForDocumentReadinessAsync(string documentId, CancellationToken cancellationToken = default)
    {
        if (!this._waitForIngestionToComplete)
        {
            return;
        }

        using var timedTokenSource = new CancellationTokenSource(this._maxIngestionWait);
        using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timedTokenSource.Token, cancellationToken);

        try
        {
            while (!await this._memory.IsDocumentReadyAsync(documentId: documentId, cancellationToken: linkedTokenSource.Token).ConfigureAwait(false))
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500), linkedTokenSource.Token).ConfigureAwait(false);
            }
        }
        catch (TaskCanceledException)
        {
            // Nothing to do
        }
    }

    private static MemoryFilter? TagsToMemoryFilter(TagCollection? tags)
    {
        if (tags == null)
        {
            return null;
        }

        var filters = new MemoryFilter();

        foreach (var tag in tags)
        {
            filters.Add(tag.Key, tag.Value);
        }

        return filters;
    }
}
