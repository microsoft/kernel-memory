// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticMemory;
using Microsoft.SemanticMemory.Handlers;

var memory = new MemoryClientBuilder()
    .WithOpenAIDefaults(Env.Var("OPENAI_API_KEY"))
    .With(new TextPartitioningOptions
    {
        MaxTokensPerParagraph = 299,
        MaxTokensPerLine = 99,
        OverlappingTokens = 47,
    })
    .BuildServerlessClient();

await memory.ImportDocumentAsync(new Document()
    .AddFile("mswordfile.docx"), steps: new[]
{
    "extract",
    "partition"
});
