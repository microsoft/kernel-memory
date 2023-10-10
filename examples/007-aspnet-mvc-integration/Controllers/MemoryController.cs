// Copyright (c) Microsoft. All rights reserved.

using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticMemory;
using Microsoft.SemanticMemory.DataFormats.Image;

namespace Controllers;

[ApiController]
[Route("[controller]")]
public class MemoryController : ControllerBase
{
    private readonly ISemanticMemoryClient _memory;
    private readonly IOcrEngine _ocr;

    public MemoryController(ISemanticMemoryClient memory, IOcrEngine ocr)
    {
        this._memory = memory;
        this._ocr = ocr;
    }

    [HttpGet(Name = "GetAnswer")]
    public async Task<string> GetAsync()
    {
        var ocrResult = await this._ocr.ExtractTextFromImageAsync(null);
        return ocrResult;
    }
}
