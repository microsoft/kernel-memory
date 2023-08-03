// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticMemory.Client;

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

// =======================
// === UPLOAD ============
// =======================

// Uploading one file - This will create
// a new upload every time because no file ID is specified, and
// stored under the "default" user because no User ID is specified.
await memory.ImportFileAsync("file1-Wikipedia-Carbon.txt");

// Uploading one file specifying IDs, only if the file has not been (successfully) uploaded
if (!await memory.ExistsAsync(userId: "user1", documentId: "f01"))
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
if (!await memory.ExistsAsync(userId: "user2", documentId: "f05"))
{
    await memory.ImportFileAsync("file5-NASA-news.pdf",
        new DocumentDetails("user2", "f05")
            .AddTag("collection", "samples")
            .AddTag("collection", "webClient")
            .AddTag("collection", ".NET")
            .AddTag("type", "news"));
}

// while (
//     !await memory.ExistsAsync(userId: "user1", documentId: "f01")
//     || !await memory.ExistsAsync(userId: "user1", documentId: "f02")
//     || !await memory.ExistsAsync(userId: "user1", documentId: "f03")
//     || !await memory.ExistsAsync(userId: "user1", documentId: "f04")
//     || !await memory.ExistsAsync(userId: "user2", documentId: "f05")
// )
// {
//     Console.WriteLine("Waiting for memory ingestion to complete...");
//     await Task.Delay(TimeSpan.FromSeconds(1));
// }

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
    Console.WriteLine($"  - {x.SourceName}  - {x.Link}");
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
    Console.WriteLine($"  - {x.SourceName}  - {x.Link}");
}
