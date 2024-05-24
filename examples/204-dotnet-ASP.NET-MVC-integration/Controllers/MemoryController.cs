// Copyright (c) Microsoft. All rights reserved.

using Microsoft.AspNetCore.Mvc;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.DataFormats;

namespace Controllers;

[ApiController]
[Route("[controller]")]
public class MemoryController : Controller
{
    private readonly IKernelMemory _memory;
    private readonly IOcrEngine _ocr;

    public MemoryController(
        IKernelMemory memory,
        IOcrEngine ocr)
    {
        this._memory = memory;
        this._ocr = ocr;
    }

    // GET http://localhost:5000/Memory
    [HttpGet]
    public async Task<string> GetAsync()
    {
        // Return data from MyOcrEngine
        var ocrResult = await this._ocr.ExtractTextFromImageAsync(new MemoryStream());
        return ocrResult;
    }
}
