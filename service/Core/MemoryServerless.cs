// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.KernelMemory.Configuration;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.Models;
using Microsoft.KernelMemory.Pipeline;
using Microsoft.KernelMemory.Search;

// ReSharper disable once CheckNamespace
namespace Microsoft.KernelMemory;

/// <summary>
/// Memory client to upload files and search for answers, without depending
/// on a web service. By design this class is hardcoded to use
/// <see cref="InProcessPipelineOrchestrator"/>, hence the name "Serverless".
/// The class accesses directly storage, vectors and AI.
/// </summary>
public class MemoryServerless : IKernelMemory
{
    private readonly ISearchClient _searchClient;
    private readonly InProcessPipelineOrchestrator _orchestrator;
    private readonly string? _defaultIndexName;

    /// <summary>
    /// Synchronous orchestrator used by the serverless memory.
    /// The property is public to allow adding synchronous handlers, e.g.
    /// - memory.Orchestrator.TryAddHandlerAsync(...)
    /// - memory.Orchestrator.AddHandlerAsync(...)
    /// - memory.Orchestrator.AddHandler(...)
    /// - memory.Orchestrator.AddHandler...(...)
    /// </summary>
    public InProcessPipelineOrchestrator Orchestrator => this._orchestrator;

    public MemoryServerless(
        InProcessPipelineOrchestrator orchestrator,
        ISearchClient searchClient,
        KernelMemoryConfig? config = null)
    {
        this._orchestrator = orchestrator ?? throw new ConfigurationException("The orchestrator is NULL");
        this._searchClient = searchClient ?? throw new ConfigurationException("The search client is NULL");

        // A non-null config object is required in order to get a non-empty default index name
        config ??= new KernelMemoryConfig();
        this._defaultIndexName = config.DefaultIndexName;
    }

    /// <inheritdoc />
    public Task<string> ImportDocumentAsync(
        Document document,
        string? index = null,
        IEnumerable<string>? steps = null,
        CancellationToken cancellationToken = default)
    {
        DocumentUploadRequest uploadRequest = new(document, index, steps);
        return this.ImportDocumentAsync(uploadRequest, cancellationToken);
    }

    /// <inheritdoc />
    public Task<string> ImportDocumentAsync(
        string filePath,
        string? documentId = null,
        TagCollection? tags = null,
        string? index = null,
        IEnumerable<string>? steps = null,
        CancellationToken cancellationToken = default)
    {
        var document = new Document(documentId, tags: tags).AddFile(filePath);
        DocumentUploadRequest uploadRequest = new(document, index, steps);
        return this.ImportDocumentAsync(uploadRequest, cancellationToken);
    }

    /// <inheritdoc />
    public Task<string> ImportDocumentAsync(
        DocumentUploadRequest uploadRequest,
        CancellationToken cancellationToken = default)
    {
        var index = IndexName.CleanName(uploadRequest.Index, this._defaultIndexName);
        return this._orchestrator.ImportDocumentAsync(index, uploadRequest, cancellationToken);
    }

    /// <inheritdoc />
    public Task<string> ImportDocumentAsync(
        Stream content,
        string? fileName = null,
        string? documentId = null,
        TagCollection? tags = null,
        string? index = null,
        IEnumerable<string>? steps = null,
        CancellationToken cancellationToken = default)
    {
        var document = new Document(documentId, tags: tags).AddStream(fileName, content);
        DocumentUploadRequest uploadRequest = new(document, index, steps);
        return this.ImportDocumentAsync(uploadRequest, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> ImportTextAsync(
        string text,
        string? documentId = null,
        TagCollection? tags = null,
        string? index = null,
        IEnumerable<string>? steps = null,
        CancellationToken cancellationToken = default)
    {
        var content = new MemoryStream(Encoding.UTF8.GetBytes(text));
        await using (content.ConfigureAwait(false))
        {
            return await this.ImportDocumentAsync(
                content,
                fileName: "content.txt",
                documentId: documentId,
                tags: tags,
                index: index,
                steps: steps,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<string> ImportWebPageAsync(
        string url,
        string? documentId = null,
        TagCollection? tags = null,
        string? index = null,
        IEnumerable<string>? steps = null,
        CancellationToken cancellationToken = default)
    {
        var uri = new Uri(url);
        Verify.ValidateUrl(uri.AbsoluteUri, requireHttps: false, allowReservedIp: false, allowQuery: true);

        Stream content = new MemoryStream(Encoding.UTF8.GetBytes(uri.AbsoluteUri));
        await using (content.ConfigureAwait(false))
        {
            return await this.ImportDocumentAsync(content, fileName: "content.url", documentId: documentId, tags: tags, index: index, steps: steps, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<IndexDetails>> ListIndexesAsync(CancellationToken cancellationToken = default)
    {
        return (from index in await this._searchClient.ListIndexesAsync(cancellationToken).ConfigureAwait(false)
                select new IndexDetails { Name = index });
    }

    /// <inheritdoc />
    public Task DeleteIndexAsync(string? index = null, CancellationToken cancellationToken = default)
    {
        index = IndexName.CleanName(index, this._defaultIndexName);
        return this._orchestrator.StartIndexDeletionAsync(index: index, cancellationToken);
    }

    /// <inheritdoc />
    public Task DeleteDocumentAsync(string documentId, string? index = null, CancellationToken cancellationToken = default)
    {
        index = IndexName.CleanName(index, this._defaultIndexName);
        return this._orchestrator.StartDocumentDeletionAsync(documentId: documentId, index: index, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> IsDocumentReadyAsync(
        string documentId,
        string? index = null,
        CancellationToken cancellationToken = default)
    {
        index = IndexName.CleanName(index, this._defaultIndexName);
        return await this._orchestrator.IsDocumentReadyAsync(index: index, documentId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<DataPipelineStatus?> GetDocumentStatusAsync(
        string documentId,
        string? index = null,
        CancellationToken cancellationToken = default)
    {
        index = IndexName.CleanName(index, this._defaultIndexName);
        try
        {
            DataPipeline? pipeline = await this._orchestrator.ReadPipelineStatusAsync(index: index, documentId, cancellationToken).ConfigureAwait(false);
            return pipeline?.ToDataPipelineStatus();
        }
        catch (PipelineNotFoundException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public Task<SearchResult> SearchAsync(
        string query,
        string? index = null,
        MemoryFilter? filter = null,
        ICollection<MemoryFilter>? filters = null,
        double minRelevance = 0,
        int limit = -1,
        CancellationToken cancellationToken = default)
    {
        if (filter != null)
        {
            if (filters == null) { filters = new List<MemoryFilter>(); }

            filters.Add(filter);
        }

        index = IndexName.CleanName(index, this._defaultIndexName);
        return this._searchClient.SearchAsync(
            index: index,
            query: query,
            filters: filters,
            minRelevance: minRelevance,
            limit: limit,
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public Task<MemoryAnswer> AskAsync(
        string question,
        string? index = null,
        MemoryFilter? filter = null,
        ICollection<MemoryFilter>? filters = null,
        double minRelevance = 0,
        CancellationToken cancellationToken = default)
    {
        if (filter != null)
        {
            if (filters == null) { filters = new List<MemoryFilter>(); }

            filters.Add(filter);
        }

        index = IndexName.CleanName(index, this._defaultIndexName);
        return this._searchClient.AskAsync(
            index: index,
            question: question,
            filters: filters,
            minRelevance: minRelevance,
            cancellationToken: cancellationToken);
    }
}
