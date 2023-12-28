// Copyright (c) Microsoft. All rights reserved.

using Microsoft.AspNetCore.Mvc;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.DataFormats;

namespace Controllers;

[ApiController]
[Route("[controller]")]
public class MemoryController : ControllerBase
{
    private readonly IKernelMemory _memory;
    private readonly IOcrEngine _ocr;

    public MemoryController(IKernelMemory memory, IOcrEngine ocr)
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
