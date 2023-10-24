// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Handlers;

var memory = new KernelMemoryBuilder()
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
