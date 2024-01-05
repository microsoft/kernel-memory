// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Configuration;

var memory = new KernelMemoryBuilder()
    .WithOpenAIDefaults(Env.Var("OPENAI_API_KEY"))
    .WithCustomTextPartitioningOptions(new TextPartitioningOptions
    {
        MaxTokensPerParagraph = 299,
        MaxTokensPerLine = 99,
        OverlappingTokens = 47,
    })
    .Build<MemoryServerless>();

await memory.ImportDocumentAsync(new Document()
    .AddFile("mswordfile.docx"), steps: new[]
{
    "extract",
    "partition"
});
