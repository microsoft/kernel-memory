// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.KernelMemory.Context;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.Models;
using Microsoft.KernelMemory.Pipeline;
using Microsoft.KernelMemory.Search;

// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

public sealed class MemoryService : IKernelMemory
{
    private readonly IPipelineOrchestrator _orchestrator;
    private readonly ISearchClient _searchClient;
    private readonly string? _defaultIndexName;

    public MemoryService(
        IPipelineOrchestrator orchestrator,
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
        IContext? context = null,
        CancellationToken cancellationToken = default)
    {
        DocumentUploadRequest uploadRequest = new(document, index, steps);
        return this.ImportDocumentAsync(uploadRequest, context, cancellationToken);
    }

    /// <inheritdoc />
    public Task<string> ImportDocumentAsync(
        string filePath,
        string? documentId = null,
        TagCollection? tags = null,
        string? index = null,
        IEnumerable<string>? steps = null,
        IContext? context = null,
        CancellationToken cancellationToken = default)
    {
        var document = new Document(documentId, tags: tags).AddFile(filePath);
        DocumentUploadRequest uploadRequest = new(document, index, steps);
        return this.ImportDocumentAsync(uploadRequest, context, cancellationToken);
    }

    /// <inheritdoc />
    public Task<string> ImportDocumentAsync(
        DocumentUploadRequest uploadRequest,
        IContext? context = null,
        CancellationToken cancellationToken = default)
    {
        var index = IndexName.CleanName(uploadRequest.Index, this._defaultIndexName);
        return this._orchestrator.ImportDocumentAsync(index, uploadRequest, context, cancellationToken);
    }

    /// <inheritdoc />
    public Task<string> ImportDocumentAsync(
        Stream content,
        string? fileName = null,
        string? documentId = null,
        TagCollection? tags = null,
        string? index = null,
        IEnumerable<string>? steps = null,
        IContext? context = null,
        CancellationToken cancellationToken = default)
    {
        var document = new Document(documentId, tags: tags).AddStream(fileName, content);
        DocumentUploadRequest uploadRequest = new(document, index, steps);
        return this.ImportDocumentAsync(uploadRequest, context, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> ImportTextAsync(
        string text,
        string? documentId = null,
        TagCollection? tags = null,
        string? index = null,
        IEnumerable<string>? steps = null,
        IContext? context = null,
        CancellationToken cancellationToken = default)
    {
        var content = new MemoryStream(Encoding.UTF8.GetBytes(text));
        await using (content.ConfigureAwait(false))
        {
            return await this.ImportDocumentAsync(
                content: content,
                fileName: "content.txt",
                documentId: documentId,
                tags: tags,
                index: index,
                steps: steps,
                context: context,
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
        IContext? context = null,
        CancellationToken cancellationToken = default)
    {
        var uri = new Uri(url);
        Verify.ValidateUrl(uri.AbsoluteUri, requireHttps: false, allowReservedIp: false, allowQuery: true);

        Stream content = new MemoryStream(Encoding.UTF8.GetBytes(uri.AbsoluteUri));
        await using (content.ConfigureAwait(false))
        {
            return await this.ImportDocumentAsync(
                    content: content,
                    fileName: "content.url",
                    documentId: documentId,
                    tags: tags,
                    index: index,
                    steps: steps,
                    context: context,
                    cancellationToken: cancellationToken)
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
    public Task<bool> IsDocumentReadyAsync(
        string documentId,
        string? index = null,
        CancellationToken cancellationToken = default)
    {
        index = IndexName.CleanName(index, this._defaultIndexName);
        return this._orchestrator.IsDocumentReadyAsync(index: index, documentId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<DataPipelineStatus?> GetDocumentStatusAsync(
        string documentId,
        string? index = null,
        CancellationToken cancellationToken = default)
    {
        index = IndexName.CleanName(index, this._defaultIndexName);
        return this._orchestrator.ReadPipelineSummaryAsync(index: index, documentId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<StreamableFileContent> ExportFileAsync(
        string documentId,
        string fileName,
        string? index = null,
        CancellationToken cancellationToken = default)
    {
        var pipeline = new DataPipeline
        {
            Index = IndexName.CleanName(index, this._defaultIndexName),
            DocumentId = documentId,
        };
        return this._orchestrator.ReadFileAsStreamAsync(pipeline, fileName, cancellationToken);
    }

    /// <inheritdoc />
    public Task<SearchResult> SearchAsync(
        string query,
        string? index = null,
        MemoryFilter? filter = null,
        ICollection<MemoryFilter>? filters = null,
        double minRelevance = 0,
        int limit = -1,
        IContext? context = null,
        CancellationToken cancellationToken = default)
    {
        if (filter != null)
        {
            if (filters == null) { filters = []; }

            filters.Add(filter);
        }

        index = IndexName.CleanName(index, this._defaultIndexName);
        return this._searchClient.SearchAsync(
            index: index,
            query: query,
            filters: filters,
            minRelevance: minRelevance,
            limit: limit,
            context: context,
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<MemoryAnswer> AskStreamingAsync(
        string question,
        string? index = null,
        MemoryFilter? filter = null,
        ICollection<MemoryFilter>? filters = null,
        double minRelevance = 0,
        SearchOptions? options = null,
        IContext? context = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (filter != null)
        {
            filters ??= [];
            filters.Add(filter);
        }

        index = IndexName.CleanName(index, this._defaultIndexName);

        if (options is { Stream: true })
        {
            await foreach (var answer in this._searchClient.AskStreamingAsync(
                               index: index,
                               question: question,
                               filters: filters,
                               minRelevance: minRelevance,
                               context: context,
                               cancellationToken).ConfigureAwait(false))
            {
                yield return answer;
            }

            yield break;
        }

        yield return await this._searchClient.AskAsync(
            index: index,
            question: question,
            filters: filters,
            minRelevance: minRelevance,
            context: context,
            cancellationToken).ConfigureAwait(false);
    }
}
