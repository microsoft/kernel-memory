// Copyright (c) Microsoft. All rights reserved.

using System;
using System.ClientModel.Primitives;
using System.Net.Http;
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.Diagnostics;

namespace Microsoft.KernelMemory.AI.AzureOpenAI.Internals;

internal static class AzureOpenAIClientBuilder
{
    internal static AzureOpenAIClient Build(
        AzureOpenAIConfig config,
        HttpClient? httpClient = null,
        ILoggerFactory? loggerFactory = null)
    {
        if (string.IsNullOrEmpty(config.Endpoint))
        {
            throw new ConfigurationException($"Azure OpenAI: config.{nameof(config.Endpoint)} is empty");
        }

        AzureOpenAIClientOptions options = new()
        {
            RetryPolicy = new ClientSequentialRetryPolicy(maxRetries: Math.Max(0, config.MaxRetries), loggerFactory),
            ApplicationId = Telemetry.HttpUserAgent,
        };

        if (httpClient is not null)
        {
            options.Transport = new HttpClientPipelineTransport(httpClient);
        }

        switch (config.Auth)
        {
            case AzureOpenAIConfig.AuthTypes.AzureIdentity:
                return new AzureOpenAIClient(endpoint: new Uri(config.Endpoint), credential: new DefaultAzureCredential(), options: options);

            case AzureOpenAIConfig.AuthTypes.ManualTokenCredential:
                return new AzureOpenAIClient(new Uri(config.Endpoint), config.GetTokenCredential(), options);

            case AzureOpenAIConfig.AuthTypes.APIKey:
                if (string.IsNullOrEmpty(config.APIKey))
                {
                    throw new ConfigurationException($"Azure OpenAI: {config.APIKey} is empty");
                }

                return new AzureOpenAIClient(new Uri(config.Endpoint), new AzureKeyCredential(config.APIKey), options);

            default:
                throw new ConfigurationException($"Azure OpenAI: authentication type '{config.Auth:G}' is not supported");
        }
    }
}
