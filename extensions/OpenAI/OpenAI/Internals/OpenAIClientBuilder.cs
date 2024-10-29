// Copyright (c) Microsoft. All rights reserved.

using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.Diagnostics;
using OpenAI;

namespace Microsoft.KernelMemory.AI.OpenAI.Internals;

internal static class OpenAIClientBuilder
{
    internal static OpenAIClient Build(
        OpenAIConfig config,
        HttpClient? httpClient = null,
        ILoggerFactory? loggerFactory = null)
    {
        OpenAIClientOptions options = new()
        {
            RetryPolicy = new ClientSequentialRetryPolicy(maxRetries: Math.Max(0, config.MaxRetries), loggerFactory),
            UserAgentApplicationId = Telemetry.HttpUserAgent,
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

        return new OpenAIClient(new ApiKeyCredential(config.APIKey), options);
    }
}
