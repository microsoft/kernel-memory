// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticMemory.Client;
using Microsoft.SemanticMemory.Client.Models;

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

while (
    !await memory.IsDocumentReadyAsync(documentId: "doc001")
    || !await memory.IsDocumentReadyAsync(documentId: "doc002")
    || !await memory.IsDocumentReadyAsync(documentId: "doc003")
)
{
    Console.WriteLine("Waiting for memory ingestion to complete...");
    await Task.Delay(TimeSpan.FromSeconds(2));
}

// =======================
// === ASK ===============
// =======================

// Question without filters
var question = "What's Semantic Kernel?";
Console.WriteLine($"\n\nQuestion: {question}");

var answer = await memory.AskAsync(question);
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
 
doc001 already uploaded.
doc002 already uploaded.
doc003 already uploaded.


Question: What's Semantic Kernel?

Answer: Semantic Kernel is a lightweight SDK that enables integration of AI Large Language Models 
(LLMs) with conventional programming languages. It combines natural language semantic functions, 
traditional code native functions, and embeddings-based memory to add value to applications with AI. 
It supports prompt templating, function chaining, vectorized memory, and intelligent planning 
capabilities out of the box. Semantic Kernel encapsulates several design patterns from the latest 
in AI research, such that developers can infuse their applications with plugins like prompt chaining, 
recursive reasoning, summarization, zero/few-shot learning, contextual memory, long-term memory, 
embeddings, semantic indexing, planning, retrieval-augmented generation and accessing external 
knowledge stores as well as your own data. It is available to explore AI and build apps with C# 
and Python.

  Sources:

  - file4-SK-Readme.pdf  - doc002/b426b222c4434d6d9c1e4a4101bfd8e3 [Monday, August 14, 2023]
  - file3-lorem-ipsum.docx  - doc002/46296fc7999e4a888f790bc81d591c54 [Monday, August 14, 2023]
  - file2-Wikipedia-Moon.txt  - doc002/d8bc5b9400704878b0bbbffa34dc504c [Monday, August 14, 2023]
  - file1-Wikipedia-Carbon.txt  - doc001/b9333cdc30a34870b2022358e327ff8c [Monday, August 14, 2023]
  - file5-NASA-news.pdf  - doc003/8b99b4534cc54a14860c15bd6c28beb2 [Monday, August 14, 2023]


Question: Any news from NASA about Orion?

Blake Answer: INFO NOT FOUND.


Taylor Answer: Yes, NASA has invited media to see the new test version of the Orion spacecraft 
and the hardware teams will use to recover the capsule and astronauts upon their return from 
space during the Artemis II mission. Personnel involved in recovery operations from NASA, the 
U.S. Navy, and the U.S. Air Force will be available to speak with media. The event will take 
place at Naval Base San Diego on August 2, 2023. Teams are currently conducting the first in 
a series of tests in the Pacific Ocean to demonstrate and evaluate the processes, procedures, 
and hardware for recovery operations for crewed Artemis missions. The tests will help prepare 
the team for Artemis II, NASA’s first crewed mission under Artemis that will send four 
astronauts in Orion around the Moon to checkout systems ahead of future lunar missions.

  Sources:

  - file5-NASA-news.pdf  - doc003/8b99b4534cc54a14860c15bd6c28beb2 [Monday, August 14, 2023]


Question: What is Orion?

Articles: INFO NOT FOUND



News: Orion is a spacecraft developed by NASA for crewed missions, including the Artemis 
program which aims to send astronauts to the Moon. NASA has invited media to see the 
recovery craft for the Artemis II mission, which will use the Orion spacecraft.

*/
