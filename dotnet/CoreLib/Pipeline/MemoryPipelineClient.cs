// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.TextEmbedding;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.Tokenizers;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticMemory.Client;
using Microsoft.SemanticMemory.Core.AppBuilders;
using Microsoft.SemanticMemory.Core.Configuration;
using Microsoft.SemanticMemory.Core.Diagnostics;
using Microsoft.SemanticMemory.Core.Handlers;
using Microsoft.SemanticMemory.Core.MemoryStorage;

namespace Microsoft.SemanticMemory.Core.Pipeline;

public class MemoryPipelineClient : ISemanticMemoryClient
{
    public MemoryPipelineClient()
        : this(SemanticMemoryConfig.LoadFromAppSettings())
    {
    }

    public MemoryPipelineClient(
        SemanticMemoryConfig config,
        ILogger<MemoryPipelineClient>? log = null)
    {
        this._config = config;
        this._log = log ?? NullLogger<MemoryPipelineClient>.Instance;

        switch (config.Search.GetEmbeddingGeneratorConfig())
        {
            case AzureOpenAIConfig cfg:
                this._embeddingGenerator = new AzureTextEmbeddingGeneration(
                    modelId: cfg.Deployment,
                    endpoint: cfg.Endpoint,
                    apiKey: cfg.APIKey,
                    logger: this._log);
                break;

            case OpenAIConfig cfg:
                this._embeddingGenerator = new OpenAITextEmbeddingGeneration(
                    modelId: cfg.Model,
                    apiKey: cfg.APIKey,
                    organization: cfg.OrgId,
                    logger: this._log);
                break;

            default:
                throw new OrchestrationException(
                    $"Unknown/unsupported embedding generator '{config.Search.EmbeddingGenerator?.GetType().FullName}'");
        }

        switch (config.Search.GetVectorDbConfig())
        {
            case AzureCognitiveSearchConfig cfg:
                this._vectorDb = new AzureCognitiveSearchMemory(
                    endpoint: cfg.Endpoint,
                    apiKey: cfg.APIKey,
                    indexPrefix: cfg.VectorIndexPrefix,
                    log: this._log);
                break;

            default:
                throw new OrchestrationException(
                    $"Unknown/unsupported vector DB '{config.Search.VectorDb?.GetType().FullName}'");
        }

        switch (config.Search.GetTextGeneratorConfig())
        {
            case AzureOpenAIConfig cfg:
                this._kernel = Kernel.Builder
                    .WithLogger(this._log)
                    // .WithAzureTextCompletionService()
                    .WithAzureChatCompletionService(
                        deploymentName: cfg.Deployment,
                        endpoint: cfg.Endpoint,
                        apiKey: cfg.APIKey).Build();
                break;

            case OpenAIConfig cfg:
                this._kernel = Kernel.Builder
                    .WithLogger(this._log)
                    .WithOpenAIChatCompletionService(
                        modelId: cfg.Model,
                        apiKey: cfg.APIKey,
                        orgId: cfg.OrgId).Build();
                break;

            default:
                throw new OrchestrationException(
                    $"Unknown/unsupported embedding generator '{config.Search.EmbeddingGenerator?.GetType().FullName}'");
        }
    }

    /// <inheritdoc />
    public async Task<string> ImportFileAsync(Document file)
    {
        var ids = await this.ImportFilesAsync(new[] { file }).ConfigureAwait(false);
        return ids.First();
    }

    /// <inheritdoc />
    public Task<IList<string>> ImportFilesAsync(Document[] files)
    {
        return this.ImportFilesInternalAsync(files);
    }

    /// <inheritdoc />
    public Task<string> ImportFileAsync(string fileName)
    {
        return this.ImportFileAsync(new Document(fileName));
    }

    /// <inheritdoc />
    public async Task<string> ImportFileAsync(string fileName, DocumentDetails details)
    {
        var ids = await this.ImportFilesAsync(new[] { new Document(fileName) { Details = details } }).ConfigureAwait(false);
        return ids.First();
    }

    /// <inheritdoc />
    public async Task<string> AskAsync(string question, string userId)
    {
        const float MinSimilarity = 0.5f;
        const int MatchesCount = 100;
        const int AnswerTokens = 300;

        string indexName = userId;

        if (this._embeddingGenerator == null) { throw new SemanticMemoryException("Embedding generator not configured"); }

        if (this._vectorDb == null) { throw new SemanticMemoryException("Search vector DB not configured"); }

        if (this._kernel == null) { throw new SemanticMemoryException("Semantic Kernel not configured"); }

        IList<Embedding<float>> embeddings = await this._embeddingGenerator
            .GenerateEmbeddingsAsync(new List<string> { question }).ConfigureAwait(false);
        Embedding<float> embedding;
        if (embeddings.Count == 0)
        {
            throw new SemanticMemoryException("Failed to generate embedding for the given question");
        }

        embedding = embeddings.First();

        var prompt = "Facts:\n" +
                     "{{$facts}}" +
                     "======\n" +
                     "Given the facts above, provide a comprehensive/detailed answer.\n" +
                     "You don't know where the knowledge comes from, just answer.\n" +
                     "Question: {{$question}}.\n" +
                     "Answer: ";

        var skFunction = this._kernel.CreateSemanticFunction(prompt.Trim(), maxTokens: AnswerTokens, temperature: 0);

        IAsyncEnumerable<(MemoryRecord, double)> matches = this._vectorDb.GetNearestMatchesAsync(
            indexName, embedding, MatchesCount, MinSimilarity, false);

        var facts = string.Empty;
        var tokensAvailable = 8000
                              - GPT3Tokenizer.Encode(prompt).Count
                              - GPT3Tokenizer.Encode(question).Count
                              - AnswerTokens;
        await foreach ((MemoryRecord, double) memory in matches)
        {
            var partition = memory.Item1.Metadata["text"].ToString()?.Trim() ?? "";
            var fact = $"======\n{partition}\n";
            var size = GPT3Tokenizer.Encode(fact).Count;
            if (size < tokensAvailable)
            {
                facts += fact;
                tokensAvailable -= size;
                continue;
            }

            break;
        }

        var context = this._kernel.CreateNewContext();
        context["facts"] = facts.Trim();
        context["question"] = question.Trim();
        SKContext result = await skFunction.InvokeAsync(context).ConfigureAwait(false);

        return result.Result;
    }

    #region private

    private readonly SemanticMemoryConfig _config;
    private readonly Lazy<Task<InProcessPipelineOrchestrator>> _inProcessOrchestrator = new(BuildInProcessOrchestratorAsync);
    private readonly ILogger<MemoryPipelineClient> _log;
    private readonly IKernel? _kernel;
    private readonly ITextEmbeddingGeneration? _embeddingGenerator;
    private readonly ISemanticMemoryVectorDb? _vectorDb;

    private Task<InProcessPipelineOrchestrator> Orchestrator
    {
        get { return this._inProcessOrchestrator.Value; }
    }

    private async Task<IList<string>> ImportFilesInternalAsync(Document[] files)
    {
        List<string> ids = new();
        InProcessPipelineOrchestrator orchestrator = await this.Orchestrator.ConfigureAwait(false);

        foreach (Document file in files)
        {
            var pipeline = orchestrator
                .PrepareNewFileUploadPipeline(
                    documentId: file.Details.DocumentId,
                    userId: file.Details.UserId, file.Details.Tags);

            pipeline.AddUploadFile(
                name: "file1",
                filename: file.FileName,
                sourceFile: file.FileName);

            pipeline
                .Then("extract")
                .Then("partition")
                .Then("gen_embeddings")
                .Then("save_embeddings")
                .Build();

            await orchestrator.RunPipelineAsync(pipeline).ConfigureAwait(false);
            ids.Add(file.Details.DocumentId);
        }

        return ids;
    }

    private static async Task<InProcessPipelineOrchestrator> BuildInProcessOrchestratorAsync()
    {
        IServiceProvider services = AppBuilder.Build().Services;

        var orchestrator = GetOrchestrator(services);

        // Text extraction handler
        TextExtractionHandler textExtraction = new("extract",
            orchestrator, GetLogger<TextExtractionHandler>(services));
        await orchestrator.AddHandlerAsync(textExtraction).ConfigureAwait(false);

        // Text partitioning handler
        TextPartitioningHandler textPartitioning = new("partition",
            orchestrator, GetLogger<TextPartitioningHandler>(services));
        await orchestrator.AddHandlerAsync(textPartitioning).ConfigureAwait(false);

        // Embedding generation handler
        GenerateEmbeddingsHandler textEmbedding = new("gen_embeddings",
            orchestrator, GetConfig(services), GetLogger<GenerateEmbeddingsHandler>(services));
        await orchestrator.AddHandlerAsync(textEmbedding).ConfigureAwait(false);

        // Embedding storage handler
        SaveEmbeddingsHandler saveEmbedding = new("save_embeddings",
            orchestrator, GetConfig(services), GetLogger<SaveEmbeddingsHandler>(services));
        await orchestrator.AddHandlerAsync(saveEmbedding).ConfigureAwait(false);

        return orchestrator;
    }

    private static InProcessPipelineOrchestrator GetOrchestrator(IServiceProvider services)
    {
        var orchestrator = services.GetService<InProcessPipelineOrchestrator>();
        if (orchestrator == null)
        {
#pragma warning disable CA2208
            throw new ArgumentNullException(nameof(orchestrator),
                $"Unable to instantiate {typeof(InProcessPipelineOrchestrator)} with AppBuilder");
#pragma warning restore CA2208
        }

        return orchestrator;
    }

    private static SemanticMemoryConfig GetConfig(IServiceProvider services)
    {
        var config = services.GetService<SemanticMemoryConfig>();
        if (config == null)
        {
            throw new OrchestrationException("Unable to load configuration, object is NULL");
        }

        return config;
    }

    private static ILogger<T>? GetLogger<T>(IServiceProvider services)
    {
        return services.GetService<ILogger<T>>();
    }

    #endregion
}
