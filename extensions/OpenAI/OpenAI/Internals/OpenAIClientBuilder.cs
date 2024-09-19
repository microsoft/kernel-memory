// Copyright (c) Microsoft. All rights reserved.

using System;
using System.ClientModel.Primitives;
using System.Net.Http;
using Microsoft.KernelMemory.Diagnostics;
using OpenAI;

namespace Microsoft.KernelMemory.AI.OpenAI;

internal static class OpenAIClientBuilder
{
    internal static OpenAIClient Build(
        OpenAIConfig config,
        HttpClient? httpClient = null)
    {
        OpenAIClientOptions options = new()
        {
            RetryPolicy = new ClientSequentialRetryPolicy(maxRetries: Math.Max(0, config.MaxRetries)),
            ApplicationId = Telemetry.HttpUserAgent,
        };

        if (httpClient is not null)
        {
            options.Transport = new HttpClientPipelineTransport(httpClient);
        }

        // Point the client to a non-OpenAI endpoint, e.g. LM Studio web service
        if (!string.IsNullOrWhiteSpace(config.Endpoint)
            && !config.Endpoint.StartsWith(ChangeEndpointPolicy.DefaultEndpoint, StringComparison.OrdinalIgnoreCase))
        {
            options.AddPolicy(new ChangeEndpointPolicy(config.Endpoint), PipelinePosition.PerTry);
        }

        return new OpenAIClient(config.APIKey, options);
    }
}
