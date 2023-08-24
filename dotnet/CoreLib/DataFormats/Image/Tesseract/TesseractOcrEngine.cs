// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using System.Threading.Tasks;
using Tesseract;

namespace Microsoft.SemanticMemory.DataFormats.Image.Tesseract;

/// <summary>
/// Wrapper for the TesseractEngine within the Tesseract OCR library
/// </summary>
public class TesseractOcrEngine : IOcrEngine
{
    private readonly TesseractEngine _tesseractEngine;

    /// <summary>
    /// Creates a new instance of the TesseractEngineWrapper passing in a valid TesseractEngine.
    /// </summary>
    /// <param name="tesseractEngine"></param>
    public TesseractOcrEngine(TesseractEngine tesseractEngine)
    {
        this._tesseractEngine = tesseractEngine ?? throw new ArgumentNullException(nameof(tesseractEngine)); ;
    }

    ///<inheritdoc/>
    public async Task<string> ExtractTextFromImageAsync(Stream imageContent)
    {
        using var memoryStream = new MemoryStream();
        await imageContent.CopyToAsync(memoryStream).ConfigureAwait(false);

        using var img = Pix.LoadFromMemory(memoryStream.ToArray());
        using var page = this._tesseractEngine.Process(img);

        return page.GetText();
    }
}
