// Copyright (c) Microsoft. All rights reserved.

using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticMemory;

namespace Controllers;

[ApiController]
[Route("[controller]")]
public class MemoryController : ControllerBase
{
    private readonly ISemanticMemoryClient _memory;

    public MemoryController(ISemanticMemoryClient memory)
    {
        this._memory = memory;
    }

    [HttpGet(Name = "GetAnswer")]
    public async Task<MemoryAnswer> GetAsync()
    {
        return await this._memory.AskAsync("What's Microsoft Copilot?");
    }
}
