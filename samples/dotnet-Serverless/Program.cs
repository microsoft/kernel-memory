// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticMemory.Core.Configuration;
using Microsoft.SemanticMemory.Core.Pipeline;
using Microsoft.SemanticMemory.Core20;
using Microsoft.SemanticMemory.InteractiveSetup;

// Run `dotnet run setup` to run this code and setup the example
if (new[] { "setup", "-setup" }.Contains(args.FirstOrDefault(), StringComparer.OrdinalIgnoreCase))
{
    Main.InteractiveSetup(cfgService: true);
}

/* Use MemoryPipelineClient to run the default import pipeline
 * in the same process, without distributed queues.
 * 
 * The pipeline might use settings in appsettings.json, but uses
 * 'InProcessPipelineOrchestrator' explicitly.
 * 
 * Note: no web service required, each file is processed in this process. */

var config = SemanticMemoryConfig.LoadFromAppSettings();

var memory = new MemoryPipelineClient(config);

// Uploading one file - This will create
// a new upload every time because no file ID is specified, and
// stored under the "default" user because no User ID is specified.
await memory.ImportFileAsync("file1.txt");

// Uploading one file specifying IDs
await memory.ImportFileAsync("file1.txt",
    new DocumentDetails(documentId: "f01", userId: "user1"));

// Uploading multiple files
await memory.ImportFilesAsync(new[]
{
    new Document("file2.txt", new DocumentDetails("f02", "user1")),
    new Document("file3.docx", new DocumentDetails("f03", "user1")),
    new Document("file4.pdf", new DocumentDetails("f04", "user1")),
});

// Categorizing files with tags
await memory.ImportFileAsync("file5.pdf",
    new DocumentDetails("f05", "user2")
        .AddTag("collection", "samples")
        .AddTag("collection", "webClient")
        .AddTag("collection", ".NET")
        .AddTag("type", "news"));

// Test with User 1 memory
var question = "What's Semantic Kernel?";
Console.WriteLine($"\n\nQuestion: {question}");

string answer = await memory.AskAsync(question, "user1");
Console.WriteLine($"Answer: {answer}");

// Test with User 2 memory
question = "Any news from NASA about Orion?";
Console.WriteLine($"\n\nQuestion: {question}");

answer = await memory.AskAsync(question, "user2");
Console.WriteLine($"Answer: {answer}");
