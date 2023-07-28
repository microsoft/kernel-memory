// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticMemory.Core.Configuration;
using Microsoft.SemanticMemory.Core20;
using Microsoft.SemanticMemory.PipelineClient;

public static class Example1_ImportWithMemoryPipelineClient
{
    public static async Task RunAsync()
    {
        var config = SemanticMemoryConfig.LoadFromAppSettings();

        var memory = new MemoryPipelineClient(config);

        await memory.ImportFileAsync("file1.txt",
            new ImportFileOptions(userId: "user1", collectionId: "collection01", documentId: "upload1"));

        await memory.ImportFilesAsync(new[] { "file2.txt", "file3.docx", "file4.pdf" },
            new ImportFileOptions(userId: "user2", collectionId: "collection01", documentId: "upload2"));

        await memory.ImportFileAsync("5.docx",
            new ImportFileOptions(userId: "user3", collectionId: "collection01", documentId: "upload1"));

        var owner = "user3";

        var question = "What's Semantic Kernel?";
        Console.WriteLine($"Question: {question}");

        string answer = await memory.AskAsync(question, owner);
        Console.WriteLine($"Answer: {answer}");
    }
}
