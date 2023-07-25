// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticKernel.SemanticMemory.Core20;
using Microsoft.SemanticKernel.SemanticMemory.SemanticMemoryWebClient;

public static class Example2_ImportWithMemoryWebClient
{
    public static async Task RunAsync(string endpoint)
    {
        MemoryWebClient memory = new(endpoint);

        await memory.ImportFileAsync("file1.txt",
            new ImportFileOptions("example2-user", "collection01"));

        await memory.ImportFilesAsync(new[] { "file2.txt", "file3.docx", "file4.pdf" },
            new ImportFileOptions("example2-user", "collection01"));

        Console.WriteLine("Question: What's SK?");
        string answer = await memory.AskAsync("What's SK?");
        Console.WriteLine($"Answer: {answer}");
    }
}
