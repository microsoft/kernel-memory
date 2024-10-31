// Copyright (c) Microsoft. All rights reserved.

using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net.Http;
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
            UserAgentApplicationId = Telemetry.HttpUserAgent,
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

                return new AzureOpenAIClient(new Uri(config.Endpoint), new ApiKeyCredential(config.APIKey), options);

            default:
                throw new ConfigurationException($"Azure OpenAI: authentication type '{config.Auth:G}' is not supported");
        }
    }
}

// Use only for local debugging - Usage:
//
// 1. Add this code in the builder above:
//
//      options.Transport = new HttpClientPipelineTransport(new HttpClient(new DebuggingHandler(new HttpClientHandler())));
//
// 2. Add these at the top:
//
//      using System.Threading;
//      using System.Threading.Tasks;
//
// 3. Uncomment this class:
//
// #pragma warning disable CA1303
// internal class DebuggingHandler : DelegatingHandler
// {
//     public DebuggingHandler(HttpMessageHandler innerHandler) : base(innerHandler)
//     {
//     }
//
//     protected override async Task<HttpResponseMessage> SendAsync(
//         HttpRequestMessage request, CancellationToken cancellationToken)
//     {
//         // Log request URI
//         Console.WriteLine("#### Request URI: " + request.RequestUri);
//
//         // Log request headers
//         Console.WriteLine("#### Request Headers:");
//         foreach (var header in request.Headers)
//         {
//             Console.WriteLine($"#### {header.Key}: {string.Join(", ", header.Value)}");
//         }
//
//         // Send the request to the inner handler
//         var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
//
//         // Optionally, log response headers here
//         Console.WriteLine("#### Response Headers:");
//         foreach (var header in response.Headers)
//         {
//             Console.WriteLine($"#### {header.Key}: {string.Join(", ", header.Value)}");
//         }
//
//         return response;
//     }
// }
