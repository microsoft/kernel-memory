// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticMemory;

/* Use MemoryServerlessClient to run the default import pipeline
 * in the same process, without distributed queues.
 *
 * The pipeline might use settings in appsettings.json, but uses
 * 'InProcessPipelineOrchestrator' explicitly.
 *
 * Note: no web service required, each file is processed in this process. */

var memory = new MemoryClientBuilder()
    .WithFilesystemStorage("tmp-storage")
    .WithOpenAIDefaults(Env.Var("OPENAI_API_KEY"))
    // .WithQdrant("http://127.0.0.1:6333")
    .WithAzureCognitiveSearch(Env.Var("ACS_ENDPOINT"), Env.Var("ACS_API_KEY"))
    .BuildServerlessClient();

// =======================
// === UPLOAD ============
// =======================

// Uploading some text, without using files
if (!await memory.IsDocumentReadyAsync(documentId: "doc000"))
{
    Console.WriteLine("Uploading doc000");
    await memory.ImportTextAsync("In physics, mass–energy equivalence is the relationship between mass and energy " +
                                 "in a system's rest frame, where the two quantities differ only by a multiplicative " +
                                 "constant and the units of measurement. The principle is described by the physicist " +
                                 "Albert Einstein's formula: E = m*c^2", documentId: "doc000");
}
else
{
    Console.WriteLine("doc000 already uploaded.");
}

// Simple file upload (checking if the file exists)
if (!await memory.IsDocumentReadyAsync(documentId: "doc001"))
{
    Console.WriteLine("Uploading doc001");
    await memory.ImportDocumentAsync("file1-Wikipedia-Carbon.txt", documentId: "doc001");
}
else
{
    Console.WriteLine("doc001 already uploaded.");
}

// Uploading multiple files and adding a user tag
if (!await memory.IsDocumentReadyAsync(documentId: "doc002"))
{
    Console.WriteLine("Uploading doc002");
    await memory.ImportDocumentAsync(new Document("doc002")
        .AddFiles(new[] { "file2-Wikipedia-Moon.txt", "file3-lorem-ipsum.docx", "file4-SK-Readme.pdf" })
        .AddTag("user", "Blake"));
}
else
{
    Console.WriteLine("doc002 already uploaded.");
}

// Categorizing files with several tags
if (!await memory.IsDocumentReadyAsync(documentId: "doc003"))
{
    Console.WriteLine("Uploading doc003");
    await memory.ImportDocumentAsync(new Document("doc003")
        .AddFile("file5-NASA-news.pdf")
        .AddTag("user", "Taylor")
        .AddTag("collection", "meetings")
        .AddTag("collection", "NASA")
        .AddTag("collection", "space")
        .AddTag("type", "news"));
}
else
{
    Console.WriteLine("doc003 already uploaded.");
}

// =======================
// === ASK ===============
// =======================

// Question without filters
var question = "What's E = m*c^2?";
Console.WriteLine($"\n\nQuestion: {question}");

var answer = await memory.AskAsync(question);
Console.WriteLine($"\nAnswer: {answer.Result}\n\n");

// Another question without filters
question = "What's Semantic Kernel?";
Console.WriteLine($"\n\nQuestion: {question}");

answer = await memory.AskAsync(question);
Console.WriteLine($"\nAnswer: {answer.Result}\n\n  Sources:\n");

foreach (var x in answer.RelevantSources)
{
    Console.WriteLine($"  - {x.SourceName}  - {x.Link} [{x.Partitions.First().LastUpdate:D}]");
}

// Filter question by "user" tag
question = "Any news from NASA about Orion?";
Console.WriteLine($"\n\nQuestion: {question}");

answer = await memory.AskAsync(question, filter: new MemoryFilter().ByTag("user", "Blake"));
Console.WriteLine($"\nBlake Answer: {answer.Result}\n");

answer = await memory.AskAsync(question, filter: new MemoryFilter().ByTag("user", "Taylor"));
Console.WriteLine($"\nTaylor Answer: {answer.Result}\n\n  Sources:\n");

foreach (var x in answer.RelevantSources)
{
    Console.WriteLine($"  - {x.SourceName}  - {x.Link} [{x.Partitions.First().LastUpdate:D}]");
}

// Filter question by "type" tag
question = "What is Orion?";
Console.WriteLine($"\n\nQuestion: {question}");

answer = await memory.AskAsync(question, filter: new MemoryFilter().ByTag("type", "article"));
Console.WriteLine($"\nArticles: {answer.Result}\n\n");

answer = await memory.AskAsync(question, filter: new MemoryFilter().ByTag("type", "news"));
Console.WriteLine($"\nNews: {answer.Result}\n\n");

// ReSharper disable CommentTypo
/* ==== OUTPUT ====

Uploading doc000
doc001 already uploaded.
doc002 already uploaded.
doc003 already uploaded.


Question: What's mc^2?

Answer: mc^2 refers to the equation E=mc^2, which is the famous mass-energy equivalence equation proposed by
Albert Einstein in his theory of relativity. In this equation, E represents energy, m represents mass, and
c represents the speed of light in a vacuum. The equation states that energy is equal to mass multiplied by
the square of the speed of light. This equation shows the relationship between mass and energy, suggesting
that mass can be converted into energy and vice versa. It is a fundamental equation in physics and has
significant implications in various fields, including nuclear energy and particle physics.


Question: What's Semantic Kernel?
warn: Microsoft.SemanticMemory.Search.SearchClient[0]
      No memories available

Answer: INFO NOT FOUND

  Sources:



Question: Any news from NASA about Orion?

Blake Answer: INFO NOT FOUND.


Taylor Answer: Yes, NASA has invited media to see the new test version of the Orion spacecraft and the 
hardware teams will use to recover the capsule and astronauts upon their return from space during the Artemis 
II mission. The event will take place at Naval Base San Diego and personnel involved in recovery operations 
from NASA, the U.S. Navy, and the U.S. Air Force will be available to speak with media. Teams are currently 
conducting the first in a series of tests in the Pacific Ocean to demonstrate and evaluate the processes, 
procedures, and hardware for recovery operations for crewed Artemis missions. The tests will help prepare 
the team for Artemis II, NASA’s first crewed mission under Artemis that will send four astronauts in Orion 
around the Moon to checkout systems ahead of future lunar missions.

  Sources:

  - file5-NASA-news.pdf  - doc003/8b99b4534cc54a14860c15bd6c28beb2 [Monday, August 14, 2023]


Question: What is Orion?

Articles: INFO NOT FOUND


warn: Microsoft.SemanticMemory.Search.SearchClient[0]
      No memories available

News: Orion is a spacecraft developed by NASA for crewed missions, including the Artemis program which aims 
to send astronauts to the Moon. NASA is currently conducting tests to prepare for the recovery of the Orion 
spacecraft and astronauts upon their return from space.
*/
