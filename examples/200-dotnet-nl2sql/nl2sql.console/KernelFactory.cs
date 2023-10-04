// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;

namespace SemanticKernel.Data.Nl2Sql;

/// <summary>
/// Responsible for initializing Semantic <see cref="Kernel"/> based on the configuration.
/// </summary>
internal static class KernelFactory
{
    // Azure settings
    private const string SettingNameAzureApiKey = "AZURE_OPENAI_KEY";
    private const string SettingNameAzureEndpoint = "AZURE_OPENAI_ENDPOINT";
    private const string SettingNameAzureModelCompletion = "AZURE_OPENAI_DEPLOYMENT_NAME";
    private const string SettingNameAzureModelEmbedding = "AZURE_OPENAI_EMBEDDINGS_DEPLOYMENT_NAME";

    // Open AI settings
    private const string SettingNameOpenAIApiKey = "OPENAI_API_KEY";
    private const string SettingNameOpenAIModelCompletion = "OPENAI_API_COMPLETION_MODEL";
    private const string SettingNameOpenAIModelEmbedding = "OPENAI_API_EMBEDDING_MODEL";

    /// <summary>
    /// Penalty for using any model less than GPT4 for SQL generation.
    /// </summary>
    private const string DefaultChatModel = "gpt-4";

    private const string DefaultEmbedModel = "text-embedding-ada-002";

    /// <summary>
    /// Factory method for <see cref="IServiceCollection"/>
    /// </summary>
    public static Func<IServiceProvider, IKernel> Create(IConfiguration configuration)
    {
        return CreateKernel;

        IKernel CreateKernel(IServiceProvider provider)
        {
            var loggerFactory = provider.GetService<ILoggerFactory>();

            var apikey = configuration.GetValue<string>(SettingNameAzureApiKey);
            if (!string.IsNullOrWhiteSpace(apikey))
            {
                var endpoint = configuration.GetValue<string>(SettingNameAzureEndpoint) ??
                               throw new InvalidDataException($"No endpoint configured in {SettingNameAzureEndpoint}.");

                var modelCompletion = configuration.GetValue<string>(SettingNameAzureModelCompletion);
                var modelEmbedding = configuration.GetValue<string>(SettingNameAzureModelEmbedding);

                return ConfigureAzure(endpoint, apikey, modelCompletion, modelEmbedding, loggerFactory).Build();
            }

            apikey = configuration.GetValue<string>(SettingNameOpenAIApiKey);
            if (!string.IsNullOrWhiteSpace(apikey))
            {
                var modelCompletion = configuration.GetValue<string>(SettingNameOpenAIModelCompletion);
                var modelEmbedding = configuration.GetValue<string>(SettingNameOpenAIModelEmbedding);

                return ConfigureOpenAI(apikey, modelCompletion, modelEmbedding, loggerFactory).Build();
            }

            throw new InvalidDataException($"No api-key configured in {SettingNameAzureApiKey} or {SettingNameOpenAIApiKey}.");
        }
    }

    private static KernelBuilder ConfigureOpenAI(
        string apikey,
        string? modelCompletion,
        string? modelEmbedding,
        ILoggerFactory? loggerFactory)
    {
        var builder = new KernelBuilder();

        if (loggerFactory != null)
        {
            builder.WithLoggerFactory(loggerFactory);
        }

        modelCompletion ??= DefaultChatModel;
        modelEmbedding ??= DefaultEmbedModel;

        builder
            .WithMemoryStorage(new VolatileMemoryStore())
            .WithOpenAITextEmbeddingGenerationService(modelEmbedding, apikey);

        if (!modelCompletion.StartsWith("gpt", StringComparison.OrdinalIgnoreCase))
        {
            return builder.WithOpenAITextCompletionService(modelCompletion, apikey);
        }

        return builder.WithOpenAIChatCompletionService(modelCompletion, apikey);
    }

    private static KernelBuilder ConfigureAzure(
        string endpoint,
        string apikey,
        string? modelCompletion,
        string? modelEmbedding,
        ILoggerFactory? loggerFactory)
    {
        var builder = new KernelBuilder();

        if (loggerFactory != null)
        {
            builder.WithLoggerFactory(loggerFactory);
        }

        modelCompletion ??= DefaultChatModel;
        modelEmbedding ??= DefaultEmbedModel;

        builder
            .WithMemoryStorage(new VolatileMemoryStore())
            .WithAzureTextEmbeddingGenerationService(modelEmbedding, endpoint, apikey);

        if (!modelCompletion.StartsWith("gpt", StringComparison.OrdinalIgnoreCase))
        {
            return builder.WithAzureTextCompletionService(modelCompletion, endpoint, apikey);
        }

        return builder.WithAzureChatCompletionService(modelCompletion, endpoint, apikey);
    }
}
