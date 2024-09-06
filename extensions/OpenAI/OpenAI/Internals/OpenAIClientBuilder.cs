// Copyright (c) Microsoft. All rights reserved.

using System;
using System.ClientModel.Primitives;
using System.Net.Http;
using OpenAI;

namespace Microsoft.KernelMemory.AI.OpenAI;

internal static class OpenAIClientBuilder
{
    internal static OpenAIClient BuildOpenAIClient(
        OpenAIConfig config,
        HttpClient? httpClient = null)
    {
        OpenAIClientOptions options = new();

        // Point the client to a non-OpenAI endpoint, e.g. LM Studio web service
        if (!string.IsNullOrWhiteSpace(config.Endpoint)
            && !config.Endpoint.StartsWith(ChangeEndpointPolicy.DefaultEndpoint, StringComparison.OrdinalIgnoreCase))
        {
            options.AddPolicy(new ChangeEndpointPolicy(config.Endpoint), PipelinePosition.PerTry);
        }
        if (httpClient is not null)
        {
            options.Transport = new HttpClientPipelineTransport(httpClient);
        }

        return new OpenAIClient(config.APIKey, options);
    }
}
