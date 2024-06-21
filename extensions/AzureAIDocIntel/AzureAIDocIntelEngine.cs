// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.Diagnostics;

namespace Microsoft.KernelMemory.DataFormats.AzureAIDocIntel;

/// <summary>
/// OCR engine based on Azure AI Document Intelligence
/// </summary>
[Experimental("KMEXP02")]
public sealed class AzureAIDocIntelEngine : IOcrEngine
{
    private readonly DocumentAnalysisClient _recognizerClient;
    private readonly ILogger<AzureAIDocIntelEngine> _log;

    /// <summary>
    /// Creates a new instance of the Azure AI Document Intelligence.
    /// </summary>
    /// <param name="config">Azure AI Document Intelligence settings</param>
    /// <param name="loggerFactory">Application logger factory</param>
    public AzureAIDocIntelEngine(
        AzureAIDocIntelConfig config,
        ILoggerFactory? loggerFactory = null)
    {
        this._log = (loggerFactory ?? DefaultLogger.Factory).CreateLogger<AzureAIDocIntelEngine>();

        switch (config.Auth)
        {
            case AzureAIDocIntelConfig.AuthTypes.AzureIdentity:
                this._recognizerClient = new DocumentAnalysisClient(new Uri(config.Endpoint), new DefaultAzureCredential());
                break;

            case AzureAIDocIntelConfig.AuthTypes.APIKey:
                if (string.IsNullOrEmpty(config.APIKey))
                {
                    this._log.LogCritical("Azure AI Document Intelligence API key is empty");
                    throw new ConfigurationException("Azure AI Document Intelligence API key is empty");
                }

                this._recognizerClient = new DocumentAnalysisClient(new Uri(config.Endpoint), new AzureKeyCredential(config.APIKey));
                break;

            default:
                this._log.LogCritical("Azure AI Document Intelligence authentication type '{0}' undefined or not supported", config.Auth);
                throw new ConfigurationException($"Azure AI Document Intelligence authentication type '{config.Auth}' undefined or not supported");
        }
    }

    ///<inheritdoc/>
    public async Task<string> ExtractTextFromImageAsync(Stream imageContent, CancellationToken cancellationToken = default)
    {
        // Start the OCR operation
        var operation = await this._recognizerClient.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-read", imageContent, cancellationToken: cancellationToken).ConfigureAwait(false);

        // Wait for the result
        Response<AnalyzeResult> operationResponse = await operation.WaitForCompletionAsync(cancellationToken).ConfigureAwait(false);

        return operationResponse.Value.Content;
    }
}
