// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.TextEmbedding;
using Microsoft.SemanticMemory.Core.AI.AzureOpenAI;
using Microsoft.SemanticMemory.Core.AI.OpenAI;
using Microsoft.SemanticMemory.Core.AppBuilders;
using Microsoft.SemanticMemory.Core.Configuration;
using Microsoft.SemanticMemory.Core.ContentStorage.AzureBlobs;
using Microsoft.SemanticMemory.Core.ContentStorage.FileSystemStorage;
using Microsoft.SemanticMemory.Core.MemoryStorage;
using Microsoft.SemanticMemory.Core.MemoryStorage.AzureCognitiveSearch;
using Microsoft.SemanticMemory.Core.MemoryStorage.Qdrant;
using Microsoft.SemanticMemory.Core.Pipeline;

/// <summary>
/// Flexible dependency injection using dependencies defined in appsettings.json
/// </summary>
public static class Builder
{
    private const string ConfigRoot = "SemanticMemory";

    public static WebApplicationBuilder CreateBuilder(out SemanticMemoryConfig config)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        config = builder.Configuration.GetSection(ConfigRoot).Get<SemanticMemoryConfig>()
                 ?? throw new ConfigurationException("Configuration is null");

        builder.Services.ConfigureRuntime(config);

        ConfigureStorage(builder, config);
        ConfigureIngestion(builder, config);
        ConfigureRetrieval(builder, config);

        return builder;
    }

    private static void ConfigureStorage(WebApplicationBuilder builder, SemanticMemoryConfig config)
    {
        // Service where documents and temporary files are stored
        switch (config.ContentStorageType)
        {
            case string x when x.Equals("AzureBlobs", StringComparison.OrdinalIgnoreCase):
                builder.Services.AddAzureBlobAsContentStorage(builder.Configuration
                    .GetSection(ConfigRoot).GetSection("Services").GetSection("AzureBlobs")
                    .Get<AzureBlobConfig>()!);
                break;

            case string x when x.Equals("FileSystemContentStorage", StringComparison.OrdinalIgnoreCase):
                builder.Services.AddFileSystemAsContentStorage(builder.Configuration
                    .GetSection(ConfigRoot).GetSection("Services").GetSection("FileSystemContentStorage")
                    .Get<FileSystemConfig>()!);
                break;

            default:
                throw new NotSupportedException($"Unknown/unsupported {config.ContentStorageType} content storage");
        }
    }

    private static void ConfigureIngestion(WebApplicationBuilder builder, SemanticMemoryConfig config)
    {
        // Hardcoded type, not configurable
        builder.Services.AddSingleton<InProcessPipelineOrchestrator, InProcessPipelineOrchestrator>();

        // List of embedding generators to use (multiple generators allowed during ingestion)
        var embeddingGenerationServices = new TypeCollection<ITextEmbeddingGeneration>();
        builder.Services.AddSingleton(embeddingGenerationServices);
        foreach (var type in config.DataIngestion.EmbeddingGeneratorTypes)
        {
            switch (type)
            {
                case string x when x.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase):
                case string y when y.Equals("AzureOpenAIEmbedding", StringComparison.OrdinalIgnoreCase):
                    embeddingGenerationServices.Add<AzureTextEmbeddingGeneration>();
                    builder.Services.AddAzureOpenAIEmbeddingGeneration(builder.Configuration
                        .GetSection(ConfigRoot).GetSection("Services").GetSection("AzureOpenAIEmbedding")
                        .Get<AzureOpenAIConfig>()!);
                    break;

                case string x when x.Equals("OpenAI", StringComparison.OrdinalIgnoreCase):
                    embeddingGenerationServices.Add<OpenAITextEmbeddingGeneration>();
                    builder.Services.AddOpenAITextEmbeddingGeneration(builder.Configuration
                        .GetSection(ConfigRoot).GetSection("Services").GetSection("OpenAI")
                        .Get<OpenAIConfig>()!);
                    break;

                default:
                    throw new NotSupportedException($"Unknown/unsupported {type} embedding generator");
            }
        }

        // List of Vector DB list where to store embeddings (multiple DBs allowed during ingestion)
        var vectorDbServices = new TypeCollection<ISemanticMemoryVectorDb>();
        builder.Services.AddSingleton(vectorDbServices);
        foreach (var type in config.DataIngestion.VectorDbTypes)
        {
            switch (type)
            {
                case string x when x.Equals("AzureCognitiveSearch", StringComparison.OrdinalIgnoreCase):
                    // Add the class for the handlers using multiple vector DBs
                    vectorDbServices.Add<AzureCognitiveSearchMemory>();
                    // Usual DI registration
                    builder.Services.AddAzureCognitiveSearchAsVectorDb(builder.Configuration
                        .GetSection(ConfigRoot).GetSection("Services").GetSection("AzureCognitiveSearch")
                        .Get<AzureCognitiveSearchConfig>()!);
                    break;

                case string x when x.Equals("Qdrant", StringComparison.OrdinalIgnoreCase):
                    // Add the class for the handlers using multiple vector DBs
                    vectorDbServices.Add<QdrantMemory>();
                    // Usual DI registration
                    builder.Services.AddQdrantAsVectorDb(builder.Configuration
                        .GetSection(ConfigRoot).GetSection("Services").GetSection("Qdrant")
                        .Get<QdrantConfig>()!);
                    break;

                default:
                    throw new NotSupportedException($"Unknown/unsupported {type} vector DB");
            }
        }
    }

    private static void ConfigureRetrieval(WebApplicationBuilder builder, SemanticMemoryConfig config)
    {
        // How to generate embeddings when searching for an answer
        switch (config.Retrieval.EmbeddingGeneratorType)
        {
            case string x when x.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase):
            case string y when y.Equals("AzureOpenAIEmbedding", StringComparison.OrdinalIgnoreCase):
                builder.Services.AddAzureOpenAIEmbeddingGeneration(builder.Configuration
                    .GetSection(ConfigRoot).GetSection("Services").GetSection("AzureOpenAIEmbedding")
                    .Get<AzureOpenAIConfig>()!);
                break;

            case string x when x.Equals("OpenAI", StringComparison.OrdinalIgnoreCase):
                builder.Services.AddOpenAITextEmbeddingGeneration(builder.Configuration
                    .GetSection(ConfigRoot).GetSection("Services").GetSection("OpenAI")
                    .Get<OpenAIConfig>()!);
                break;

            default:
                throw new NotSupportedException($"Unknown/unsupported {config.Retrieval.EmbeddingGeneratorType} text generator");
        }

        // Where to search embeddings when searching for an answer
        switch (config.Retrieval.VectorDbType)
        {
            case string x when x.Equals("AzureCognitiveSearch", StringComparison.OrdinalIgnoreCase):
                builder.Services.AddAzureCognitiveSearchAsVectorDb(builder.Configuration
                    .GetSection(ConfigRoot).GetSection("Services").GetSection("AzureCognitiveSearch")
                    .Get<AzureCognitiveSearchConfig>()!);
                break;

            default:
                throw new NotSupportedException($"Unknown/unsupported {config.Retrieval.VectorDbType} vector DB");
        }

        // How to generate an answer
        switch (config.Retrieval.TextGeneratorType)
        {
            case string x when x.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase):
            case string y when y.Equals("AzureOpenAIText", StringComparison.OrdinalIgnoreCase):
                builder.Services.AddAzureOpenAITextGeneration(builder.Configuration
                    .GetSection(ConfigRoot).GetSection("Services").GetSection("AzureOpenAIText")
                    .Get<AzureOpenAIConfig>()!);
                break;

            case string x when x.Equals("OpenAI", StringComparison.OrdinalIgnoreCase):
                builder.Services.AddOpenAITextGeneration(builder.Configuration
                    .GetSection(ConfigRoot).GetSection("Services").GetSection("OpenAI")
                    .Get<OpenAIConfig>()!);
                break;

            default:
                throw new NotSupportedException($"Unknown/unsupported {config.Retrieval.TextGeneratorType} text generator");
        }
    }
}
