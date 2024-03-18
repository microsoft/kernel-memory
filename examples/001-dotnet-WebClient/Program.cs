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
        Console.WriteLine("Uploading a text file, a Word doc, and a PDF about Kernel Memory");
        await memory.ImportDocumentAsync(new Document("doc002")
            .AddFiles(new[] { "file2-Wikipedia-Moon.txt", "file3-lorem-ipsum.docx", "file4-KM-Readme.pdf" })
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
    question = "What's Kernel Memory?";
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
Uploading a text file, a Word doc, and a PDF about Kernel Memory
Uploading a PDF with a news about NASA and Orion
Uploading https://raw.githubusercontent.com/microsoft/kernel-memory/main/README.md
Uploading https://raw.githubusercontent.com/microsoft/kernel-memory/main/docs/security/security-filters.md

====================================

Waiting for memory ingestion to complete...
Waiting for memory ingestion to complete...
Waiting for memory ingestion to complete...

====================================

Question: What's E = m*c^2?

Answer: E = m*c^2 is Albert Einstein's famous equation expressing the principle of mass–energy equivalence. This equation states that energy (E) equals mass (m) multiplied by the speed of light (c) squared. It implies that mass and energy are interchangeable; they are different forms of the same thing. In this formula, the speed of light (c) is a constant that is approximately equal to 299,792,458 meters per second. This equation is a fundamental concept in physics and has important implications in various fields, including nuclear physics and cosmology.

====================================

Question: What's Kernel Memory?

Answer: Kernel Memory (KM) is a multi-modal AI Service that specializes in the efficient indexing of datasets through custom continuous data hybrid pipelines. It supports various features such as Retrieval Augmented Generation (RAG), synthetic memory, prompt engineering, and custom semantic memory processing. KM is designed to work with advanced embeddings and Large Language Models (LLMs) to enable natural language querying, providing answers from indexed data with citations and links to original sources.

KM includes a GPT Plugin, web clients, a .NET library for embedded applications, and is available as a Docker container. It is designed for seamless integration with Semantic Kernel, Microsoft Copilot, and ChatGPT, enhancing data-driven features in applications built for popular AI platforms.

Kernel Memory is built on the feedback and lessons learned from developing Semantic Kernel (SK) and Semantic Memory (SM). It offers several features that simplify tasks such as storing files, extracting text from files, securing user data, and more. The KM codebase is entirely in .NET, allowing it to be used from any language, tool, or platform, including browser extensions and ChatGPT assistants.

KM supports a variety of data formats, including web pages, PDFs, images, Word, PowerPoint, Excel, Markdown, text, JSON, and more. It also offers a range of search capabilities, such as cosine similarity and hybrid search with filters and AND/OR conditions. KM can be used with various storage engines and vector storage solutions, and it provides features like document storage

 Sources:

 - file4-KM-Readme.pdf  - default/doc002/92a8d0ee2e1646858fcc4d682fd2ca8d [Tuesday, February 27, 2024]
 - https://raw.githubusercontent.com/microsoft/kernel-memory/main/README.md [Tuesday, February 27, 2024]
 - https://raw.githubusercontent.com/microsoft/kernel-memory/main/docs/security/security-filters.md [Tuesday, February 27, 2024]
 - file3-lorem-ipsum.docx  - default/doc002/943c9dde131e4e09911118f2e5e22f07 [Tuesday, February 27, 2024]
 - content.txt  - default/d2cd29f2cdfd46eaaa0e4fb483e03f54202402271228588843230/5cf47bd8a826472db322f26492dea138 [Tuesday, February 27, 2024]
 - file2-Wikipedia-Moon.txt  - default/doc002/975b675969034019a54be0c365bb7982 [Tuesday, February 27, 2024]

====================================

Question: Which conference is Microsoft sponsoring?

Answer: Microsoft is sponsoring the Automotive News World Congress 2023 event, which is taking place in Detroit, Michigan on September 12, 2023.

 Sources:

 - file6-ANWC-image.jpg  - default/img001/f4bd27bb0a584795b406bf394bcb5684 [Tuesday, February 27, 2024]
 - file4-KM-Readme.pdf  - default/doc002/92a8d0ee2e1646858fcc4d682fd2ca8d [Tuesday, February 27, 2024]
 - https://raw.githubusercontent.com/microsoft/kernel-memory/main/README.md [Tuesday, February 27, 2024]
 - file5-NASA-news.pdf  - default/doc003/78a4ddf2e2f041b4a55dbea584793931 [Tuesday, February 27, 2024]
 - file3-lorem-ipsum.docx  - default/doc002/943c9dde131e4e09911118f2e5e22f07 [Tuesday, February 27, 2024]
 - https://raw.githubusercontent.com/microsoft/kernel-memory/main/docs/security/security-filters.md [Tuesday, February 27, 2024]


====================================

Question: Any news from NASA about Orion?

Blake Answer (none expected): Yes, there is news from NASA about the Orion spacecraft. NASA has invited the media to see a new test version of the Orion spacecraft and the hardware that will be used to recover the capsule and astronauts upon their return from space during the Artemis II mission. The event is scheduled to take place at Naval Base San Diego on Wednesday, August 2, at 11 a.m. PDT. Personnel from NASA, the U.S. Navy, and the U.S. Air Force will be available to speak with the media.

Teams are currently conducting tests in the Pacific Ocean to demonstrate and evaluate the processes, procedures, and hardware for recovery operations for crewed Artemis missions. These tests will help prepare the team for Artemis II, which will be NASA's first crewed mission under the Artemis program. The Artemis II crew, consisting of NASA astronauts Reid Wiseman, Victor Glover, and Christina Koch, and Canadian Space Agency astronaut Jeremy Hansen, will participate in recovery testing at sea next year. For more information about the Artemis program, you can visit the NASA website.

Taylor Answer: Yes, there is news from NASA regarding the Orion spacecraft. NASA has invited media to view the new test version of the Orion spacecraft and the hardware that will be used to recover the capsule and astronauts upon their return from space during the Artemis II mission. This event is scheduled to take place at 11 a.m. PDT on Wednesday, August 2, at Naval Base San Diego.

NASA and Department of Defense personnel have been practicing recovery operations aboard the USS John P. Murtha using a crew module test article. These tests are part of a series being conducted in the Pacific Ocean to demonstrate and evaluate the processes, procedures, and hardware for recovery operations of crewed Artemis missions. The goal is to prepare the team for Artemis II, which will be NASA’s first crewed mission under the Artemis program. Artemis II aims to send four astronauts in the Orion spacecraft around the Moon to check out systems ahead of future lunar missions.

The Artemis II crew consists of NASA astronauts Reid Wiseman, Victor Glover, and Christina Koch, along with CSA (Canadian Space Agency) astronaut Jeremy Hansen. They will participate in recovery testing at sea next year.

For more information about the Artemis program, NASA has provided a link: https://www.nasa.gov/artemis.
 Sources:

 - file5-NASA-news.pdf  - default/doc003/78a4ddf2e2f041b4a55dbea584793931 [Tuesday, February 27, 2024]


====================================

Question: What is Orion?

Articles (none expected): INFO NOT FOUND

News: Orion is NASA's spacecraft designed for human deep space exploration. It is part of NASA's Artemis program, which aims to return humans to the Moon and eventually send them to Mars and beyond. The Orion spacecraft is built to take astronauts farther into space than ever before, provide emergency abort capability, sustain the crew during space travel, and provide safe re-entry from deep space return velocities. It is a critical component of NASA's plans to establish a sustainable human presence on the Moon to prepare for missions to Mars. The Artemis II mission, which will be the first crewed mission of the Artemis program, will send four astronauts in the Orion spacecraft around the Moon to test its systems ahead of future lunar missions.

====================================

Deleting memories derived from d2cd29f2cdfd46eaaa0e4fb483e03f54202402271228588843230
Deleting memories derived from doc001
Deleting memories derived from img001
Deleting memories derived from doc002
Deleting memories derived from doc003
Deleting memories derived from webPage1
Deleting memories derived from webPage2


*/
