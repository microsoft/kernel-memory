// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticKernel.SemanticMemory.Core20;
using Microsoft.SemanticKernel.SemanticMemory.SemanticMemoryPipelineClient;

public static class Example1_ImportWithMemoryClient
{
    public static async Task RunAsync()
    {
        var memory = new MemoryPipelineClient();

        await memory.ImportFileAsync("file1.txt", new ImportFileOptions("example1-user", "collection01"));
        await memory.ImportFilesAsync(new[] { "file2.txt", "file3.docx", "file4.pdf" }, new ImportFileOptions("example1-user", "collection01"));
    }
}
