// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;

/* Use MemoryWebClient to run the default import pipeline
 * deployed as a web service at "http://127.0.0.1:9001/".
 *
 * Note: start the Kernel Memory service before running this.
 * Note: if the web service uses distributed handlers, make sure
 *       handlers are running to get the pipeline to complete,
 *       otherwise the web service might just upload the files
 *       without extracting memories. */

var memory = new MemoryWebClient("http://127.0.0.1:9001/");

// Use these boolean to enable/disable parts of the examples below
bool ingestion = true;
bool useImages = true; // Deploy Azure AI Document Intelligence to use this
bool retrieval = true;
bool purge = true;

// =======================
// === INGESTION =========
// =======================

var toDelete = new List<string>();
if (ingestion)
{
    Console.WriteLine("\n====================================\n");

    // Uploading some text, without using files. Hold a copy of the ID to delete it later.
    Console.WriteLine("Uploading text about E=mc^2");
    var docId = await memory.ImportTextAsync("In physics, mass–energy equivalence is the relationship between mass and energy " +
                                             "in a system's rest frame, where the two quantities differ only by a multiplicative " +
                                             "constant and the units of measurement. The principle is described by the physicist " +
                                             "Albert Einstein's formula: E = m*c^2");
    toDelete.Add(docId);

    // Simple file upload, with document ID
    toDelete.Add("doc001");
    Console.WriteLine("Uploading article file about Carbon");
    await memory.ImportDocumentAsync("file1-Wikipedia-Carbon.txt", documentId: "doc001");

    // Extract memory from images (if OCR enabled)
    if (useImages)
    {
        toDelete.Add("img001");
        Console.WriteLine("Uploading Image file with a news about a conference sponsored by Microsoft");
        await memory.ImportDocumentAsync(new Document("img001").AddFiles(new[] { "file6-ANWC-image.jpg" }));
    }

    // Uploading multiple files and adding a user tag, checking if the document already exists
    toDelete.Add("doc002");
    if (!await memory.IsDocumentReadyAsync(documentId: "doc002"))
    {
        Console.WriteLine("Uploading a text file, a Word doc, and a PDF about Semantic Kernel");
        await memory.ImportDocumentAsync(new Document("doc002")
            .AddFiles(new[] { "file2-Wikipedia-Moon.txt", "file3-lorem-ipsum.docx", "file4-SK-Readme.pdf" })
            .AddTag("user", "Blake"));
    }
    else
    {
        Console.WriteLine("doc002 already uploaded.");
    }

    // Categorizing files with several tags
    toDelete.Add("doc003");
    if (!await memory.IsDocumentReadyAsync(documentId: "doc003"))
    {
        Console.WriteLine("Uploading a PDF with a news about NASA and Orion");
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

    // Downloading web pages
    toDelete.Add("webPage1");
    if (!await memory.IsDocumentReadyAsync("webPage1"))
    {
        Console.WriteLine("Uploading https://raw.githubusercontent.com/microsoft/kernel-memory/main/README.md");
        await memory.ImportWebPageAsync("https://raw.githubusercontent.com/microsoft/kernel-memory/main/README.md", documentId: "webPage1");
    }
    else
    {
        Console.WriteLine("webPage1 already uploaded.");
    }

    // Custom pipelines, e.g. excluding summarization
    toDelete.Add("webPage2");
    if (!await memory.IsDocumentReadyAsync("webPage2"))
    {
        Console.WriteLine("Uploading https://raw.githubusercontent.com/microsoft/kernel-memory/main/docs/security/security-filters.md");
        await memory.ImportWebPageAsync("https://raw.githubusercontent.com/microsoft/kernel-memory/main/docs/security/security-filters.md",
            documentId: "webPage2",
            steps: Constants.PipelineWithoutSummary);
    }
    else
    {
        Console.WriteLine("webPage2 already uploaded.");
    }
}

// =======================
// === WATCH PROGRESS ====
// =======================

Console.WriteLine("\n====================================\n");

foreach (var docId in toDelete)
{
    while (!await memory.IsDocumentReadyAsync(documentId: docId))
    {
        Console.WriteLine("Waiting for memory ingestion to complete...");
        await Task.Delay(TimeSpan.FromSeconds(2));
    }
}

// =======================
// === RETRIEVAL =========
// =======================

if (retrieval)
{
    Console.WriteLine("\n====================================\n");

    // Question without filters
    var question = "What's E = m*c^2?";
    Console.WriteLine($"Question: {question}");

    var answer = await memory.AskAsync(question);
    Console.WriteLine($"\nAnswer: {answer.Result}");

    Console.WriteLine("\n====================================\n");

    // Another question without filters
    question = "What's Semantic Kernel?";
    Console.WriteLine($"Question: {question}");

    answer = await memory.AskAsync(question);
    Console.WriteLine($"\nAnswer: {answer.Result}\n\n  Sources:\n");

    // Show sources / citations
    foreach (var x in answer.RelevantSources)
    {
        Console.WriteLine(x.SourceUrl != null
            ? $"  - {x.SourceUrl} [{x.Partitions.First().LastUpdate:D}]"
            : $"  - {x.SourceName}  - {x.Link} [{x.Partitions.First().LastUpdate:D}]");
    }

    if (useImages)
    {
        Console.WriteLine("\n====================================\n");
        question = "Which conference is Microsoft sponsoring?";
        Console.WriteLine($"Question: {question}");

        answer = await memory.AskAsync(question);
        Console.WriteLine($"\nAnswer: {answer.Result}\n\n  Sources:\n");

        // Show sources / citations
        foreach (var x in answer.RelevantSources)
        {
            Console.WriteLine(x.SourceUrl != null
                ? $"  - {x.SourceUrl} [{x.Partitions.First().LastUpdate:D}]"
                : $"  - {x.SourceName}  - {x.Link} [{x.Partitions.First().LastUpdate:D}]");
        }
    }

    Console.WriteLine("\n====================================\n");

    // Filter question by "user" tag
    question = "Any news from NASA about Orion?";
    Console.WriteLine($"Question: {question}");

    // Blake doesn't know
    answer = await memory.AskAsync(question, filter: MemoryFilters.ByTag("user", "Blake"));
    Console.WriteLine($"\nBlake Answer (none expected): {answer.Result}");

    // Taylor knows
    answer = await memory.AskAsync(question, filter: MemoryFilters.ByTag("user", "Taylor"));
    Console.WriteLine($"\nTaylor Answer: {answer.Result}\n  Sources:\n");

    // Show sources / citations
    foreach (var x in answer.RelevantSources)
    {
        Console.WriteLine(x.SourceUrl != null
            ? $"  - {x.SourceUrl} [{x.Partitions.First().LastUpdate:D}]"
            : $"  - {x.SourceName}  - {x.Link} [{x.Partitions.First().LastUpdate:D}]");
    }

    Console.WriteLine("\n====================================\n");

    // Filter question by "type" tag, there are news but no articles
    question = "What is Orion?";
    Console.WriteLine($"Question: {question}");

    answer = await memory.AskAsync(question, filter: MemoryFilters.ByTag("type", "article"));
    Console.WriteLine($"\nArticles (none expected): {answer.Result}");

    answer = await memory.AskAsync(question, filter: MemoryFilters.ByTag("type", "news"));
    Console.WriteLine($"\nNews: {answer.Result}");
}

// =======================
// === PURGE =============
// =======================

if (purge)
{
    Console.WriteLine("====================================");

    foreach (var docId in toDelete)
    {
        Console.WriteLine($"Deleting memories derived from {docId}");
        await memory.DeleteDocumentAsync(docId);
    }
}

// ReSharper disable CommentTypo
/* ==== OUTPUT ====

====================================

Uploading text about E=mc^2
Uploading article file about Carbon
Uploading Image file with a news about a conference sponsored by Microsoft
Uploading a text file, a Word doc, and a PDF about Semantic Kernel
Uploading a PDF with a news about NASA and Orion
Uploading https://raw.githubusercontent.com/microsoft/kernel-memory/main/README.md
Uploading https://raw.githubusercontent.com/microsoft/kernel-memory/main/docs/security/security-filters.md

====================================

Waiting for memory ingestion to complete...
Waiting for memory ingestion to complete...
Waiting for memory ingestion to complete...

====================================

Question: What's E = m*c^2?

Answer: In physics, E = m*c^2 is the formula for mass-energy equivalence, which describes the relationship between mass and energy in a system's rest frame, where the two quantities differ only by a multiplicative constant and the units of measurement.

====================================

Question: What's Semantic Kernel?

Answer: Semantic Kernel (SK) is a lightweight SDK that enables integration of AI Large Language Models (LLMs) with conventional programming languages. The SK extensible programming model combines natural language semantic functions, traditional code native functions, and embeddings-based memory unlocking new potential and adding value to applications with AI. SK supports prompt templating, function chaining, vectorized memory, and intelligent planning capabilities out of the box. By joining the SK community, developers can build AI-first apps faster and have a front-row peek at how the SDK is being built.

Sources:

- file4-SK-Readme.pdf  - doc002/d5fe4f03416b43479429b90b63cedd79 [Friday, December 8, 2023]
- content.url  - webPage1/bab2145e40a240eda3e0a24f24f10703 [Friday, December 8, 2023]
- content.url  - webPage2/2724f84d7298495585fcedf94327ca30 [Friday, December 8, 2023]

====================================

Question: Which conference is Microsoft sponsoring?

Answer: Microsoft is sponsoring the Automotive News World Congress 2023 event in Detroit, Michigan on September 12, 2023.

Sources:

- file6-ANWC-image.jpg  - img001/9e1c3829e4364617a898abf1a4641535 [Friday, December 8, 2023]
- ANWC-image-for-OCR.jpg  - ItParsesTextFromImages/2a3a933c23a740399a532aae44025f4c [Friday, December 1, 2023]
- ANWC-image-for-OCR.jpg  - ItUsesTextFoundInsideImages/7cb72bfb686e449b8dbbd6967b90a493 [Wednesday, December 6, 2023]
- file5-NASA-news.pdf  - doc003/1f9ce815e60b4c2eafe99ddf289f5609 [Friday, December 8, 2023]
- NASA-news.pdf  - NASA001/ede5c76987e74e5a8fc81e3549e5763f [Thursday, December 7, 2023]
- content.url  - webPage1/bab2145e40a240eda3e0a24f24f10703 [Friday, December 8, 2023]
- file4-SK-Readme.pdf  - doc002/d5fe4f03416b43479429b90b63cedd79 [Friday, December 8, 2023]
- file3-lorem-ipsum.docx  - doc002/efeb64b6aa2a4a418026447e5397d367 [Friday, December 8, 2023]

====================================

Question: Any news from NASA about Orion?

Blake Answer (none expected): INFO NOT FOUND.

Taylor Answer: Yes, NASA has invited media to see the new test version of the Orion spacecraft and the hardware teams will use to recover the capsule and astronauts upon their return from space during the Artemis II mission. Recovery operations personnel from NASA, the U.S. Navy, and the U.S. Air Force will be available to speak with media. Teams are currently conducting the first in a series of tests in the Pacific Ocean to demonstrate and evaluate the processes, procedures, and hardware for recovery operations for crewed Artemis missions. The tests will help prepare the team for Artemis II, NASA’s first crewed mission under Artemis that will send four astronauts in Orion around the Moon to checkout systems ahead of future lunar missions. The Artemis II crew will participate in recovery testing at sea next year.
Sources:

- file5-NASA-news.pdf  - doc003/1f9ce815e60b4c2eafe99ddf289f5609 [Friday, December 8, 2023]

====================================

Question: What is Orion?

Articles (none expected): INFO NOT FOUND

News: Orion is a spacecraft developed by NASA for crewed missions, including the Artemis program which aims to send astronauts to the Moon. NASA is currently conducting tests to evaluate the recovery operations and hardware for crewed Artemis missions, including the recovery of the Orion spacecraft and astronauts upon their return from space. The Artemis II crew, which includes NASA astronauts Reid Wiseman, Victor Glover, and Christina Koch, and CSA astronaut Jeremy Hansen, will participate in recovery testing at sea next year.
====================================
Deleting memories derived from cbcc6f3019e04fab8d08a2029501e508202312070437150015410
Deleting memories derived from doc001
Deleting memories derived from img001
Deleting memories derived from doc002
Deleting memories derived from doc003
Deleting memories derived from webPage1
Deleting memories derived from webPage2

*/
