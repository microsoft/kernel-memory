// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Net.Http;
using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Core.Pipeline;
using Microsoft.KernelMemory.Diagnostics;
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
            options.AddPolicy(new ChangeEndpointPolicy(config.Endpoint), HttpPipelinePosition.PerRetry);
        }

        if (httpClient is not null)
        {
            options.Transport = new HttpClientTransport(httpClient);
        }

        return new OpenAIClient(config.APIKey, options);
    }
}
