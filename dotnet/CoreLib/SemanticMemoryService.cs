// Copyright (c) Microsoft. All rights reserved.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticMemory.Client.Models;
using Microsoft.SemanticMemory.Core.Pipeline;
using Microsoft.SemanticMemory.Core.Search;
using Microsoft.SemanticMemory.Core.WebService;

namespace Microsoft.SemanticMemory.Core;

public interface ISemanticMemoryService
{
    /// <summary>
    /// Upload a file and start the processing pipeline
    /// </summary>
    /// <param name="uploadDetails">Details about the file and how to import it</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    /// <returns>Import Id</returns>
    Task<string> UploadFileAsync(UploadRequest uploadDetails, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch the pipeline status from storage
    /// </summary>
    /// <param name="userId">Primary user who the data belongs to. Other users, e.g. sharing, is not supported in the pipeline at this time.</param>
    /// <param name="documentId">Id of the document and pipeline execution instance</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    /// <returns>Pipeline status if available</returns>
    Task<DataPipeline?> ReadPipelineStatusAsync(string userId, string documentId, CancellationToken cancellationToken = default);

    Task<MemoryAnswer> AskAsync(SearchRequest request, CancellationToken cancellationToken = default);
}

public class SemanticMemoryService : ISemanticMemoryService
{
    private readonly IPipelineOrchestrator _orchestrator;
    private readonly SearchClient _searchClient;

    public SemanticMemoryService(
        IPipelineOrchestrator orchestrator,
        SearchClient searchClient)
    {
        this._orchestrator = orchestrator;
        this._searchClient = searchClient;
    }

    ///<inheritdoc />
    public Task<string> UploadFileAsync(
        UploadRequest uploadDetails,
        CancellationToken cancellationToken = default)
    {
        return this._orchestrator.UploadFileAsync(uploadDetails, cancellationToken);
    }

    ///<inheritdoc />
    public Task<DataPipeline?> ReadPipelineStatusAsync(string userId, string documentId, CancellationToken cancellationToken = default)
    {
        return this._orchestrator.ReadPipelineStatusAsync(userId, documentId, cancellationToken);
    }

    ///<inheritdoc />
    public Task<MemoryAnswer> AskAsync(SearchRequest request, CancellationToken cancellationToken = default)
    {
        return this._searchClient.SearchAsync(request, cancellationToken);
    }
}
