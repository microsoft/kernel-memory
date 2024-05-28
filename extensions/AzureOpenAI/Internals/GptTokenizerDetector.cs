// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Services;

namespace Microsoft.KernelMemory.AI.OpenAI.Internals;

internal static class GptTokenizerDetector
{
    internal static GptTokenizer GetTokenizer(bool isTextModel, string deployment, OpenAIClient client, ILogger logger)
    {
        //To determine tokenizer we need to perform one generation and then determine
        //the model from the response text.
        string? model = null;
        if (isTextModel)
        {
            var openaiOptions = new CompletionsOptions
            {
                DeploymentName = deployment,
                MaxTokens = 50,
                Temperature = 0.0f,
            };
            openaiOptions.Prompts.Add("answer OK!");
            var answer = client.GetCompletions(openaiOptions, cancellationToken: CancellationToken.None);
            //now we have a raw response from where we can retrieve the model
            var rawResponse = answer.GetRawResponse().Content.ToString();
            ChatCompletion? chatCompletion = JsonSerializer.Deserialize<ChatCompletion>(rawResponse);
            model = chatCompletion?.Model;
        }
        else
        {
            var openaiOptions = new ChatCompletionsOptions
            {
                DeploymentName = deployment,
                MaxTokens = 50,
                Temperature = 0.0f,
            };
            openaiOptions.Messages.Add(new ChatRequestUserMessage("answer OK!"));
            var answer = client.GetChatCompletions(openaiOptions, cancellationToken: CancellationToken.None);

            //now we have a raw response from where we can retrieve the model
            var rawResponse = answer.GetRawResponse().Content.ToString();
            ChatCompletion? chatCompletion = JsonSerializer.Deserialize<ChatCompletion>(rawResponse);
            model = chatCompletion?.Model;
        }
        return GetTokenizerFromModelName(logger, model);
    }

    internal static GptTokenizer GetTokenizerForEmbeddingModel(AzureOpenAITextEmbeddingGenerationService client, ILogger logger)
    {
        var model = client.GetModelId();
        return GetTokenizerFromModelName(logger, model);
    }

    private static GptTokenizer GetTokenizerFromModelName(ILogger logger, string? model)
    {
        if (!string.IsNullOrEmpty(model))
        {
            try
            {
                return new GptTokenizer(model);
            }
            catch (ArgumentException ex)
            {
                //This can happen if the answer model is not correct and cannot be converted to tokenizer.
                //in this situation we are more conservative, we simply log the error, and then return a default
                //Gpt3 tokenizer that is the cl100k model.
                logger.LogError(ex, "Error converting OpenaAI model {0} to tokenizer", model);
            }
        }
        return new GptTokenizer("gpt3");
    }

    private class ChatCompletion
    {
        [JsonPropertyName("model")]
        public string? Model { get; set; }
    }
}
