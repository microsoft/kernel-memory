// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Plugins.Memory;

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
    public static Func<IServiceProvider, ISemanticTextMemory> CreateMemory(IConfiguration configuration)
    {
        return CreateMemory;

        ISemanticTextMemory CreateMemory(IServiceProvider provider)
        {
            var builder = new MemoryBuilder();

            var loggerFactory = provider.GetService<ILoggerFactory>();
            if (loggerFactory != null)
            {
                builder.WithLoggerFactory(loggerFactory);
            }

            builder.WithMemoryStore(new VolatileMemoryStore());

            var apikey = configuration.GetValue<string>(SettingNameAzureApiKey);
            if (!string.IsNullOrWhiteSpace(apikey))
            {
                var endpoint = configuration.GetValue<string>(SettingNameAzureEndpoint) ??
                               throw new InvalidDataException($"No endpoint configured in {SettingNameAzureEndpoint}.");

                var modelEmbedding =
                    configuration.GetValue<string>(SettingNameAzureModelEmbedding) ??
                    DefaultEmbedModel;

                builder.WithAzureTextEmbeddingGenerationService(modelEmbedding, endpoint, apikey);

                return builder.Build();
            }

            apikey = configuration.GetValue<string>(SettingNameOpenAIApiKey);
            if (!string.IsNullOrWhiteSpace(apikey))
            {
                var modelEmbedding =
                    configuration.GetValue<string>(SettingNameOpenAIModelEmbedding) ??
                    DefaultEmbedModel;

                builder.WithOpenAITextEmbeddingGenerationService(modelEmbedding, apikey);

                return builder.Build();
            }

            throw new InvalidDataException($"No api-key configured in {SettingNameAzureApiKey} or {SettingNameOpenAIApiKey}.");
        }
    }

    /// <summary>
    /// Factory method for <see cref="IServiceCollection"/>
    /// </summary>
    public static Func<IServiceProvider, IKernel> CreateKernel(IConfiguration configuration)
    {
        return CreateKernel;

        IKernel CreateKernel(IServiceProvider provider)
        {
            var builder = new KernelBuilder();

            var loggerFactory = provider.GetService<ILoggerFactory>();
            if (loggerFactory != null)
            {
                builder.WithLoggerFactory(loggerFactory);
            }

            var apikey = configuration.GetValue<string>(SettingNameAzureApiKey);
            if (!string.IsNullOrWhiteSpace(apikey))
            {
                var endpoint = configuration.GetValue<string>(SettingNameAzureEndpoint) ??
                               throw new InvalidDataException($"No endpoint configured in {SettingNameAzureEndpoint}.");

                var modelCompletion =
                    configuration.GetValue<string>(SettingNameAzureModelCompletion) ??
                    DefaultChatModel;

                if (!modelCompletion.StartsWith("gpt", StringComparison.OrdinalIgnoreCase))
                {
                    builder.WithAzureTextCompletionService(modelCompletion, endpoint, apikey);
                }

                builder.WithAzureChatCompletionService(modelCompletion, endpoint, apikey);

                return builder.Build();
            }

            apikey = configuration.GetValue<string>(SettingNameOpenAIApiKey);
            if (!string.IsNullOrWhiteSpace(apikey))
            {
                var modelCompletion =
                    configuration.GetValue<string>(SettingNameOpenAIModelCompletion) ??
                    DefaultChatModel;

                if (!modelCompletion.StartsWith("gpt", StringComparison.OrdinalIgnoreCase))
                {
                    builder.WithOpenAITextCompletionService(modelCompletion, apikey);
                }

                builder.WithOpenAIChatCompletionService(modelCompletion, apikey);

                return builder.Build();
            }

            throw new InvalidDataException($"No api-key configured in {SettingNameAzureApiKey} or {SettingNameOpenAIApiKey}.");
        }
    }
}
