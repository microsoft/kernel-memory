// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticMemory;
using Microsoft.SemanticMemory.Handlers;

var memory = new MemoryClientBuilder()
    .WithOpenAIDefaults(Env.Var("OPENAI_API_KEY"))
    .With(new TextPartitioningOptions
    {
        MaxTokensPerLine = 100,
        MaxTokensPerParagraph = 300,
        OverlappingTokens = 50,
    })
    .BuildServerlessClient();

await memory.ImportDocumentAsync(new Document()
    .AddFile("mswordfile.docx"), steps: new[]
{
    "extract",
    "partition"
});
