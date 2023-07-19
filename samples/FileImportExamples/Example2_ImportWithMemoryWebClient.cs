// Copyright (c) Microsoft. All rights reserved.

using System.Threading.Tasks;
using Microsoft.SemanticKernel.SemanticMemory.Core20;

public static class Example2_ImportWithMemoryWebClient
{
    public static async Task RunAsync(string endpoint)
    {
        var memory = new SemanticMemoryWebClient(endpoint);

        await memory.ImportFileAsync("file1.txt", new ImportFileOptions("example2-user", "collection01"));
        await memory.ImportFilesAsync(new[] { "file2.txt", "file3.docx", "file4.pdf" }, new ImportFileOptions("example2-user", "collection01"));
    }
}
