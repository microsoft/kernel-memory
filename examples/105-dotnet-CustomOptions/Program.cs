// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticMemory;
using Microsoft.SemanticMemory.Handlers;

var memory = new MemoryClientBuilder()
    .WithOpenAIDefaults(Env.Var("OPENAI_API_KEY"))
    .WithOption(new TextPartitioningOption()
    {
        MaxTokensPerLine = 100,
        MaxTokensPerParagraph = 300,
        OverlappingTokens = 50,
    })
    .BuildServerlessClient();

await memory.ImportDocumentAsync(new Document()
    .AddFile("mswordfile.docx"), steps: new string[]
    {
        "extract",
        "partition"
    });
