// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticMemory.Client;
using Microsoft.SemanticMemory.Client.Models;
using Microsoft.SemanticMemory.Core.Diagnostics;
using Microsoft.SemanticMemory.Core.MemoryStorage;

namespace Microsoft.SemanticMemory.Core.Search;

public class MemoryClient
{
    private readonly ISemanticMemoryVectorDb _vectorDb;
    private readonly ITextEmbeddingGeneration _embeddingGenerator;
    private readonly ILogger<MemoryClient> _log;

    public MemoryClient(
        ISemanticMemoryVectorDb vectorDb,
        ITextEmbeddingGeneration embeddingGenerator,
        ILogger<MemoryClient>? log = null)
    {
        this._vectorDb = vectorDb ?? throw new SemanticMemoryException("Search vector DB not configured"); ;
        this._embeddingGenerator = embeddingGenerator ?? throw new SemanticMemoryException("Embedding generator not configured"); ;
        this._log = log ?? DefaultLogger<MemoryClient>.Instance;
    }

    public async IAsyncEnumerable<(MemoryAnswer.Citation, MemoryAnswer.Citation.Partition)> QueryMemoryAsync(
        string query,
        string indexName,
        float minSimilarity,
        int maxMatches,
        [EnumeratorCancellation]
        CancellationToken cancellationToken)
    {
        var embedding = await this.GenerateEmbeddingAsync(query).ConfigureAwait(false);

        this._log.LogTrace("Fetching relevant memories");
        var matches = this._vectorDb.GetNearestMatchesAsync(
            indexName, embedding, maxMatches, minSimilarity, withEmbeddings: false, cancellationToken);

        var memories = new Dictionary<string, MemoryAnswer.Citation>();

        await foreach ((MemoryRecord memory, double relevance) in matches.WithCancellation(cancellationToken))
        {
            if (!memory.Tags.ContainsKey(Constants.ReservedPipelineIdTag))
            {
                this._log.LogError("The memory record is missing the '{0}' tag", Constants.ReservedPipelineIdTag);
            }

            if (!memory.Tags.ContainsKey(Constants.ReservedFileIdTag))
            {
                this._log.LogError("The memory record is missing the '{0}' tag", Constants.ReservedFileIdTag);
            }

            if (!memory.Tags.ContainsKey(Constants.ReservedFileTypeTag))
            {
                this._log.LogError("The memory record is missing the '{0}' tag", Constants.ReservedFileTypeTag);
            }

            // Note: a document can be composed by multiple files
            string documentId = memory.Tags[Constants.ReservedPipelineIdTag].FirstOrDefault() ?? string.Empty;

            // Identify the file in case there are multiple files
            string fileId = memory.Tags[Constants.ReservedFileIdTag].FirstOrDefault() ?? string.Empty;

            string linkToFile = $"{documentId}/{fileId}";

            var partitionText = memory.Metadata["text"].ToString()?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(partitionText))
            {
                this._log.LogError("The document partition is empty, user: {0}, doc: {1}", memory.Owner, memory.Id);
                continue;
            }

            //// TODO: add file age in days, to push relevance of newer documents

            // If the file is already in the list of citations, only add the partition
            if (!memories.TryGetValue(linkToFile, out var citation))
            {
                string fileContentType = memory.Tags[Constants.ReservedFileTypeTag].FirstOrDefault() ?? string.Empty;
                string fileName = memory.Metadata["file_name"].ToString() ?? string.Empty;

                citation = new MemoryAnswer.Citation();
                citation.Link = linkToFile;
                citation.SourceContentType = fileContentType;
                citation.SourceName = fileName;

                memories.Add(linkToFile, citation);
            }

            // Add the partition to the list of citation

#pragma warning disable CA1806 // it's ok if parsing fails
            DateTimeOffset.TryParse(memory.Metadata["last_update"].ToString(), out var lastUpdate);
#pragma warning restore CA1806

            var partition =
                new MemoryAnswer.Citation.Partition
                {
                    Text = partitionText,
                    Relevance = Convert.ToSingle(relevance),
                    SizeInTokens = 0, // TODO: from metadata
                    LastUpdate = lastUpdate,
                };

            citation.Partitions.Add(partition);

            yield return (citation, partition);
        }
    }

    private async Task<Embedding<float>> GenerateEmbeddingAsync(string text)
    {
        this._log.LogTrace("Generating embedding for the query");
        IList<Embedding<float>> embeddings = await this._embeddingGenerator
            .GenerateEmbeddingsAsync(new List<string> { text }).ConfigureAwait(false);
        if (embeddings.Count == 0)
        {
            throw new SemanticMemoryException("Failed to generate embedding for the given question");
        }

        return embeddings.First();
    }
}
