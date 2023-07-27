// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticMemory.Core20;
using Microsoft.SemanticMemory.PipelineClient;

public static class Example1_ImportWithMemoryPipelineClient
{
    public static async Task RunAsync()
    {
        var memory = new MemoryPipelineClient();

        await memory.ImportFileAsync("file1.txt",
            new ImportFileOptions("example1-user", "collection01", "upload1"));

        await memory.ImportFilesAsync(new[] { "file2.txt", "file3.docx", "file4.pdf" },
            new ImportFileOptions("example1-user", "collection01", "upload2"));

        Console.WriteLine("Question: What's SK?");
        string answer = await memory.AskAsync("What's SK?");
        Console.WriteLine($"Answer: {answer}");
    }
}
