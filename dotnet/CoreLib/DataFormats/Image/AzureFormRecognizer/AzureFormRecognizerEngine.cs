// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.AI.FormRecognizer;
using Azure.AI.FormRecognizer.Models;

namespace Microsoft.SemanticMemory.DataFormats.Image.AzureFormRecognizer;

/// <summary>
/// Wrapper for the Azure.AI.FormRecognizer.
/// </summary>
public class AzureFormRecognizerEngine : IOcrEngine
{
    private readonly FormRecognizerClient formRecognizerClient;

    /// <summary>
    /// Creates a new instance of the AzureFormRecognizerOcrEngine passing in the Form Recognizer endpoint and key.
    /// </summary>
    /// <param name="endpoint">The endpoint for accessing a provisioned Azure Form Recognizer instance</param>
    /// <param name="credential">The AzureKeyCredential containing the provisioned Azure Form Recognizer access key</param>
    public AzureFormRecognizerEngine(string endpoint, AzureKeyCredential credential)
    {
        this.formRecognizerClient = new FormRecognizerClient(new Uri(endpoint), credential);
    }

    ///<inheritdoc/>
    public async Task<string> ExtractTextFromImageAsync(Stream imageContent)
    {
        // Start the OCR operation
        RecognizeContentOperation operation = await this.formRecognizerClient.StartRecognizeContentAsync(imageContent).ConfigureAwait(false);

        // Wait for the result
        Response<FormPageCollection> operationResponse = await operation.WaitForCompletionAsync().ConfigureAwait(false);
        FormPageCollection formPages = operationResponse.Value;

        StringBuilder text = new();

        foreach (FormPage page in formPages)
        {
            foreach (FormLine line in page.Lines)
            {
                string lineText = string.Join(" ", line.Words.Select(word => word.Text));
                text.AppendLine(lineText);
            }
        }

        return text.ToString();
    }
}
