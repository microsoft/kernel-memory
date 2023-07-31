// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticMemory.Client;
using Microsoft.SemanticMemory.InteractiveSetup;

// Run `dotnet run setup` to run this code and setup the example
if (new[] { "setup", "-setup" }.Contains(args.FirstOrDefault(), StringComparer.OrdinalIgnoreCase))
{
    Main.InteractiveSetup(cfgService: true);
}

/* Use SemanticMemoryWebClient to run the default import pipeline
 * deployed as a web service at "http://127.0.0.1:9001/".
 *
 * Note: start the semantic memory service before running this.
 * Note: if the web service uses distributed handlers, make sure
 *       handlers are running to get the pipeline to complete,
 *       otherwise the web service might just upload the files
 *       without extracting memories. */

var endpoint = "http://127.0.0.1:9001/";
MemoryWebClient memory = new(endpoint);

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

// TODO: wait for pipelines to complete

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
