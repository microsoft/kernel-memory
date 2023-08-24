// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using System.Threading.Tasks;
using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;

namespace Microsoft.SemanticMemory.DataFormats.Image.AzureFormRecognizer;

/// <summary>
/// OCR engine based on Azure.AI.FormRecognizer.
/// </summary>
public class AzureFormRecognizerEngine : IOcrEngine
{
    private readonly DocumentAnalysisClient recognizerClient;

    /// <summary>
    /// Creates a new instance of the AzureFormRecognizerOcrEngine passing in the Form Recognizer endpoint and key.
    /// </summary>
    /// <param name="endpoint">The endpoint for accessing a provisioned Azure Form Recognizer instance</param>
    /// <param name="credential">The AzureKeyCredential containing the provisioned Azure Form Recognizer access key</param>
    public AzureFormRecognizerEngine(string endpoint, AzureKeyCredential credential)
    {
        this.recognizerClient = new DocumentAnalysisClient(new Uri(endpoint), credential);
    }

    ///<inheritdoc/>
    public async Task<string> ExtractTextFromImageAsync(Stream imageContent)
    {
        // Start the OCR operation
        var operation = await this.recognizerClient.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-read", imageContent).ConfigureAwait(false);

        // Wait for the result
        Response<AnalyzeResult> operationResponse = await operation.WaitForCompletionAsync().ConfigureAwait(false);

        return operationResponse.Value.Content;
    }
}
