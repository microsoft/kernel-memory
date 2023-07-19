// Copyright (c) Microsoft. All rights reserved.

using System.Threading.Tasks;
using Microsoft.SemanticKernel.SemanticMemory.Core;
using Microsoft.SemanticKernel.SemanticMemory.Core20;

public class Example1_ImportWithMemoryClient
{
    public static async Task RunAsync()
    {
        var memory = new SemanticMemoryClient();

        await memory.ImportFileAsync("file1.txt", new ImportFileOptions("example1-user", "collection01"));
        await memory.ImportFilesAsync(new[] { "file2.txt", "file3.docx", "file4.pdf" }, new ImportFileOptions("example1-user", "collection01"));
    }
}
