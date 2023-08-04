// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticMemory.Client.Models;
using Microsoft.SemanticMemory.Core.Pipeline;
using Microsoft.SemanticMemory.InteractiveSetup;

// Run `dotnet run setup` to run this code and setup the example
if (new[] { "setup", "-setup" }.Contains(args.FirstOrDefault(), StringComparer.OrdinalIgnoreCase))
{
    Main.InteractiveSetup(cfgService: false, cfgOrchestration: false);
}

/* Use MemoryServerlessClient to run the default import pipeline
 * in the same process, without distributed queues.
 *
 * The pipeline might use settings in appsettings.json, but uses
 * 'InProcessPipelineOrchestrator' explicitly.
 *
 * Note: no web service required, each file is processed in this process. */

var memory = new MemoryServerlessClient();

// =======================
// === UPLOAD ============
// =======================

// Uploading one file - This will create
// a new upload every time because no file ID is specified, and
// stored under the "default" user because no User ID is specified.
await memory.ImportFileAsync("file1-Wikipedia-Carbon.txt");

// Uploading only if the file has not been (successfully) uploaded already
if (!await memory.IsReadyAsync(userId: "user1", documentId: "f01"))
{
    await memory.ImportFileAsync("file1-Wikipedia-Carbon.txt",
        new DocumentDetails(userId: "user1", documentId: "f01"));
}

// Uploading multiple files
await memory.ImportFilesAsync(new[]
{
    new Document("file2-Wikipedia-Moon.txt", new DocumentDetails("user1", "f02")),
    new Document("file3-lorem-ipsum.docx", new DocumentDetails("user1", "f03")),
    new Document("file4-SK-Readme.pdf", new DocumentDetails("user1", "f04")),
});

// Categorizing files with tags
if (!await memory.IsReadyAsync(userId: "user2", documentId: "f05"))
{
    await memory.ImportFileAsync("file5-NASA-news.pdf",
        new DocumentDetails("user2", "f05")
            .AddTag("collection", "samples")
            .AddTag("collection", "webClient")
            .AddTag("collection", ".NET")
            .AddTag("type", "news"));
}

// =======================
// === ASK ===============
// =======================

// Test with User 1 memory
var question = "What's Semantic Kernel?";
Console.WriteLine($"\n\nQuestion: {question}");

var answer = await memory.AskAsync("user1", question);
Console.WriteLine($"\nAnswer: {answer.Result}\n\n  Sources:\n");

foreach (var x in answer.RelevantSources)
{
    Console.WriteLine($"  - {x.SourceName}  - {x.Link} [{x.Partitions.First().LastUpdate:D}]");
}

// Test with User 2 memory
question = "Any news from NASA about Orion?";
Console.WriteLine($"\n\nQuestion: {question}");

answer = await memory.AskAsync("user1", question);
Console.WriteLine($"\nUser 1 Answer: {answer.Result}\n");

answer = await memory.AskAsync("user2", question);
Console.WriteLine($"\nUser 2 Answer: {answer.Result}\n\n  Sources:\n");

foreach (var x in answer.RelevantSources)
{
    Console.WriteLine($"  - {x.SourceName}  - {x.Link} [{x.Partitions.First().LastUpdate:D}]");
}
