// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticMemory.Configuration;

namespace Microsoft.SemanticMemory.DataFormats.Image.AzureFormRecognizer;

/// <summary>
/// OCR engine based on Azure.AI.FormRecognizer.
/// </summary>
public class AzureFormRecognizerEngine : IOcrEngine
{
    private readonly DocumentAnalysisClient _recognizerClient;
    private readonly ILogger<AzureFormRecognizerEngine> _log;

    /// <summary>
    /// Creates a new instance of the AzureFormRecognizerOcrEngine passing in the Form Recognizer endpoint and key.
    /// </summary>
    /// <param name="endpoint">The endpoint for accessing a provisioned Azure Form Recognizer instance</param>
    /// <param name="config">The AzureFormRecognizerConfig config for this service.</param>
    public AzureFormRecognizerEngine(string endpoint, AzureFormRecognizerConfig config, ILogger<AzureFormRecognizerEngine> log)
    {
        this._log = log;

        switch (config.Auth)
        {
            case AzureFormRecognizerConfig.AuthTypes.AzureIdentity:
                this._recognizerClient = new DocumentAnalysisClient(new Uri(endpoint), new DefaultAzureCredential());
                break;

            case AzureFormRecognizerConfig.AuthTypes.APIKey:
                if (string.IsNullOrEmpty(config.APIKey))
                {
                    this._log.LogCritical("Azure Form Recognizer API key is empty");
                    throw new ConfigurationException("Azure Form Recognizer API key is empty");
                }

                this._recognizerClient = new DocumentAnalysisClient(new Uri(endpoint), new AzureKeyCredential(config.APIKey));
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
