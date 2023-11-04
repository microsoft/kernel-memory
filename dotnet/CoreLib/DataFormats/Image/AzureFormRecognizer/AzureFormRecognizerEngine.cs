// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.Configuration;
using Microsoft.KernelMemory.Diagnostics;

namespace Microsoft.KernelMemory.DataFormats.Image.AzureFormRecognizer;

/// <summary>
/// OCR engine based on Azure.AI.FormRecognizer.
/// </summary>
public class AzureFormRecognizerEngine : IOcrEngine
{
    private readonly DocumentAnalysisClient _recognizerClient;
    private readonly ILogger<AzureFormRecognizerEngine> _log;

    /// <summary>
    /// Creates a new instance of the Azure Form Recognizer.
    /// </summary>
    /// <param name="config">The AzureFormRecognizerConfig config for this service</param>
    /// <param name="log">Application logger</param>
    public AzureFormRecognizerEngine(
        AzureFormRecognizerConfig config,
        ILogger<AzureFormRecognizerEngine>? log = null)
    {
        this._log = log ?? DefaultLogger<AzureFormRecognizerEngine>.Instance;

        switch (config.Auth)
        {
            case AzureFormRecognizerConfig.AuthTypes.AzureIdentity:
                this._recognizerClient = new DocumentAnalysisClient(new Uri(config.Endpoint), new DefaultAzureCredential());
                break;

            case AzureFormRecognizerConfig.AuthTypes.APIKey:
                if (string.IsNullOrEmpty(config.APIKey))
                {
                    this._log.LogCritical("Azure Form Recognizer API key is empty");
                    throw new ConfigurationException("Azure Form Recognizer API key is empty");
                }

                this._recognizerClient = new DocumentAnalysisClient(new Uri(config.Endpoint), new AzureKeyCredential(config.APIKey));
                break;

            default:
                this._log.LogCritical("Azure Form Recognizer authentication type '{0}' undefined or not supported", config.Auth);
                throw new ConfigurationException($"Azure Form Recognizer authentication type '{config.Auth}' undefined or not supported");
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
