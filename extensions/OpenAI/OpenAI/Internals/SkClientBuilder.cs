// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OpenAI;

namespace Microsoft.KernelMemory.AI.OpenAI.Internals;

internal static class SkClientBuilder
{
    internal static OpenAIChatCompletionService BuildChatClient(
        string modelId,
        OpenAIClient client,
        ILoggerFactory? loggerFactory = null)
    {
        if (string.IsNullOrEmpty(modelId))
        {
            throw new ConfigurationException("OpenAI: Model ID is empty");
        }

        return new OpenAIChatCompletionService(modelId, client, loggerFactory: loggerFactory);
    }

    internal static OpenAITextEmbeddingGenerationService BuildEmbeddingClient(
        string modelId,
        OpenAIClient client,
        int? dimensions = null,
        ILoggerFactory? loggerFactory = null)
    {
        if (string.IsNullOrEmpty(modelId))
        {
            throw new ConfigurationException("OpenAI: Model ID is empty");
        }

        if (dimensions is < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(dimensions), "Dimensions value cannot be less than 1");
        }

        return new OpenAITextEmbeddingGenerationService(modelId, client, loggerFactory, dimensions);
    }
}
