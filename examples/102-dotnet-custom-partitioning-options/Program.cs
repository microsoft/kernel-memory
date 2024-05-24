// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Configuration;

var memory = new KernelMemoryBuilder()
    .WithOpenAIDefaults(Environment.GetEnvironmentVariable("OPENAI_API_KEY")!)
    .WithCustomTextPartitioningOptions(new TextPartitioningOptions
    {
        // Max 99 tokens per sentence
        MaxTokensPerLine = 99,
        // When sentences are merged into paragraphs (aka partitions), stop at 299 tokens
        MaxTokensPerParagraph = 299,
        // Each paragraph contains the last 47 tokens from the previous one
        OverlappingTokens = 47,
    })
    .Build<MemoryServerless>();

await memory.ImportDocumentAsync(new Document()
    .AddFile("mswordfile.docx"), steps: ["extract", "partition"]);
