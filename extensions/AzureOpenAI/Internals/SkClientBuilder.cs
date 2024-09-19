// Copyright (c) Microsoft. All rights reserved.

using System;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

namespace Microsoft.KernelMemory.AI.AzureOpenAI.Internals;

internal static class SkClientBuilder
{
    internal static AzureOpenAIChatCompletionService BuildChatClient(
        string deploymentName,
        AzureOpenAIClient client,
        ILoggerFactory? loggerFactory = null)
    {
        if (string.IsNullOrEmpty(deploymentName))
        {
            throw new ConfigurationException("Azure OpenAI: Deployment Name is empty");
        }

        return new AzureOpenAIChatCompletionService(deploymentName, client, loggerFactory: loggerFactory);
    }

    internal static AzureOpenAITextEmbeddingGenerationService BuildEmbeddingClient(
        string deploymentName,
        AzureOpenAIClient client,
        int? dimensions = null,
        ILoggerFactory? loggerFactory = null)
    {
        if (string.IsNullOrEmpty(deploymentName))
        {
            throw new ConfigurationException("Azure OpenAI: Deployment Name is empty");
        }

        if (dimensions is < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(dimensions), "Dimensions value cannot be less than 1");
        }

        return new AzureOpenAITextEmbeddingGenerationService(deploymentName, client, loggerFactory: loggerFactory, dimensions: dimensions);
    }
}
