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
            new ImportFileOptions(userId: "user1", collectionId: "collection01", documentId: "doc1"));

        await memory.ImportFilesAsync(new[] { "file2.txt", "file3.docx", "file4.pdf" },
            new ImportFileOptions(userId: "user2", collectionId: "collection01", documentId: "doc2"));

        await memory.ImportFileAsync("file5.pdf",
            new ImportFileOptions(userId: "user3", collectionId: "collection01", documentId: "doc1"));

        // Test with User 2 memory
        var question = "\n\nWhat's Semantic Kernel?";
        Console.WriteLine($"Question: {question}");

        string answer = await memory.AskAsync(question, "user2");
        Console.WriteLine($"Answer: {answer}");

        // Test with User 3 memory
        question = "\n\nAny news from NASA about Orion?";
        Console.WriteLine($"Question: {question}");

        answer = await memory.AskAsync(question, "user3");
        Console.WriteLine($"Answer: {answer}");
    }
}
