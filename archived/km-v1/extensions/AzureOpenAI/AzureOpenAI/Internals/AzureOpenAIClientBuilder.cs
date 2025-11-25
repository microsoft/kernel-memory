// Copyright (c) Microsoft. All rights reserved.

using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography;
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

        var clientOptions = GetClientOptions(config, httpClient, loggerFactory);
        switch (config.Auth)
        {
            case AzureOpenAIConfig.AuthTypes.AzureIdentity:
                return new AzureOpenAIClient(endpoint: new Uri(config.Endpoint), credential: new DefaultAzureCredential(), clientOptions);

            case AzureOpenAIConfig.AuthTypes.ManualTokenCredential:
                return new AzureOpenAIClient(new Uri(config.Endpoint), config.GetTokenCredential(), clientOptions);

            case AzureOpenAIConfig.AuthTypes.APIKey:
                if (string.IsNullOrEmpty(config.APIKey))
                {
                    throw new ConfigurationException($"Azure OpenAI: {config.APIKey} is empty");
                }

                return new AzureOpenAIClient(new Uri(config.Endpoint), new ApiKeyCredential(config.APIKey), clientOptions);

            default:
                throw new ConfigurationException($"Azure OpenAI: authentication type '{config.Auth:G}' is not supported");
        }
    }

    /// <summary>
    /// Options used by the Azure OpenAI client, e.g. Retry strategy, User Agent, SSL certs, Auth tokens audience, etc.
    /// </summary>
    private static AzureOpenAIClientOptions GetClientOptions(
        AzureOpenAIConfig config,
        HttpClient? httpClient = null,
        ILoggerFactory? loggerFactory = null)
    {
        AzureOpenAIClientOptions options = new()
        {
            RetryPolicy = new ClientSequentialRetryPolicy(maxRetries: Math.Max(0, config.MaxRetries), loggerFactory),
            UserAgentApplicationId = Telemetry.HttpUserAgent,
        };

        // Custom audience for sovereign clouds. See:
        // - https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/openai/Azure.AI.OpenAI/README.md
        // - https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/openai/Azure.AI.OpenAI/src/Custom/AzureOpenAIAudience.cs
        if (config.Auth == AzureOpenAIConfig.AuthTypes.AzureIdentity && !string.IsNullOrEmpty(config.AzureIdentityAudience))
        {
            options.Audience = new AzureOpenAIAudience(config.AzureIdentityAudience);
        }

        // Azure SDK bug fix
        // See https://github.com/Azure/azure-sdk-for-net/issues/46109
        options.AddPolicy(new SingleAuthorizationHeaderPolicy(), PipelinePosition.PerTry);

        // Remote SSL certs verification
        if (httpClient is null && config.TrustedCertificateThumbprints.Count > 0)
        {
#pragma warning disable CA2000 // False Positive: https://github.com/dotnet/roslyn-analyzers/issues/4636
            httpClient = BuildHttpClientWithCustomCertificateValidation(config);
#pragma warning restore CA2000
        }

        if (httpClient is not null)
        {
            options.Transport = new HttpClientPipelineTransport(httpClient);
        }

        return options;
    }

    private static HttpClient BuildHttpClientWithCustomCertificateValidation(AzureOpenAIConfig config)
    {
#pragma warning disable CA2000 // False Positive: https://github.com/dotnet/roslyn-analyzers/issues/4636
        var handler = new HttpClientHandler();
#pragma warning restore CA2000

        handler.ClientCertificateOptions = ClientCertificateOption.Manual;
        handler.ServerCertificateCustomValidationCallback =
            (_, cert, _, policyErrors) =>
            {
                // Pass if there are no policy errors.
                if (policyErrors == SslPolicyErrors.None) { return true; }

                // Attempt to get the thumbprint of the remote certificate.
                string? remoteCert;
                try
                {
                    remoteCert = cert?.GetCertHashString();
                }
                catch (CryptographicException)
                {
                    // Fail if crypto lib is not working
                    return false;
                }
                catch (ArgumentException)
                {
                    // Fail if thumbprint format is invalid
                    return false;
                }

                // Fail if no thumbprint available
                if (remoteCert == null) { return false; }

                // Success if the remote cert thumbprint matches any of the trusted ones.
                return config.TrustedCertificateThumbprints.Any(
                    trustedCert => string.Equals(remoteCert, trustedCert, StringComparison.OrdinalIgnoreCase));
            };

        return new HttpClient(handler);
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
