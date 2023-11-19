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
bool useImages = true; // Enable Azure Form Recognizer OCR to use this
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
        Console.WriteLine("Uploading https://raw.githubusercontent.com/microsoft/kernel-memory/main/docs/SECURITY_FILTERS.md");
        await memory.ImportWebPageAsync("https://raw.githubusercontent.com/microsoft/kernel-memory/main/docs/SECURITY_FILTERS.md",
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

    foreach (var x in answer.RelevantSources)
    {
        Console.WriteLine($"  - {x.SourceName}  - {x.Link} [{x.Partitions.First().LastUpdate:D}]");
    }

    if (useImages)
    {
        Console.WriteLine("\n====================================\n");
        question = "Which conference is Microsoft sponsoring?";
        Console.WriteLine($"Question: {question}");

        answer = await memory.AskAsync(question);
        Console.WriteLine($"\nAnswer: {answer.Result}\n\n  Sources:\n");

        foreach (var x in answer.RelevantSources)
        {
            Console.WriteLine($"  - {x.SourceName}  - {x.Link} [{x.Partitions.First().LastUpdate:D}]");
        }
    }

    Console.WriteLine("\n====================================\n");

    // Filter question by "user" tag
    question = "Any news from NASA about Orion?";
    Console.WriteLine($"Question: {question}");

    // Blake doesn't know
    answer = await memory.AskAsync(question, filter: MemoryFilters.ByTag("user", "Blake"));
    Console.WriteLine($"\nBlake Answer: {answer.Result}");

    // Taylor knows
    answer = await memory.AskAsync(question, filter: MemoryFilters.ByTag("user", "Taylor"));
    Console.WriteLine($"\nTaylor Answer: {answer.Result}\n  Sources:\n");

    foreach (var x in answer.RelevantSources)
    {
        Console.WriteLine($"  - {x.SourceName}  - {x.Link} [{x.Partitions.First().LastUpdate:D}]");
    }

    Console.WriteLine("\n====================================\n");

    // Filter question by "type" tag, there are news but no articles
    question = "What is Orion?";
    Console.WriteLine($"Question: {question}");

    answer = await memory.AskAsync(question, filter: MemoryFilters.ByTag("type", "article"));
    Console.WriteLine($"\nArticles: {answer.Result}");

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

Uploading text about E=mc^2
Uploading article file about Carbon
Uploading Image file with a news about a conference sponsored by Microsoft
Uploading a text file, a Word doc, and a PDF about Semantic Kernel
Uploading a PDF with a news about NASA and Orion
Uploading https://raw.githubusercontent.com/microsoft/kernel-memory/main/README.md

====================================

Waiting for memory ingestion to complete...
Waiting for memory ingestion to complete...
Waiting for memory ingestion to complete...
Waiting for memory ingestion to complete...
Waiting for memory ingestion to complete...
Waiting for memory ingestion to complete...

====================================

Question: What's E = m*c^2?

Answer: E = m*c^2 is the formula for mass-energy equivalence in physics, where mass and energy are equivalent in a system's rest frame, differing only by a multiplicative constant and the units of measurement.

====================================

Question: What's Semantic Kernel?

Answer: Semantic Kernel is a lightweight SDK that allows integration of AI Large Language Models with conventional programming languages. It combines natural language semantic functions, traditional code native functions, and embeddings-based memory to add value to applications with AI. The SK community is invited to contribute to the development of the SDK through GitHub Discussions, opening GitHub Issues, sending PRs, and joining the Discord community. SK supports prompt templating, function chaining, vectorized memory, and intelligent planning capabilities out of the box. The SDK supports several design patterns from the latest in AI research, such as prompt chaining, recursive reasoning, summarization, zero/few-shot learning, contextual memory, long-term memory, embeddings, semantic indexing, planning, retrieval-augmented generation, and accessing external knowledge stores as well as your own data.

  Sources:

  - file4-SK-Readme.pdf  - doc002/136dec405e694b199bd62bb3b2195453 [Tuesday, August 29, 2023]
  - content.url  - webPage1/acca787af5bc4294b103bb583b31d3da [Tuesday, August 29, 2023]
  - file3-lorem-ipsum.docx  - doc002/78590246af224918a0a96e96d34c8f38 [Tuesday, August 29, 2023]
  - content.txt  - 988f0db29c114ed38267980f1af4bb26202308280750056313730/d9c41b08e22547dfa66899b48b75b2b8 [Tuesday, August 29, 2023]

====================================

Question: Which conference is Microsoft sponsoring?

Answer: Microsoft is sponsoring the Automotive News World Congress 2023 event in Detroit on September 12, 2023.

  Sources:

  - file6-ANWC-image.jpg  - img001/a4c04abf11344c9790640b00714c3177 [Tuesday, August 29, 2023]
  - content.url  - webPage1/701ca43b9bbd40a3b4500c31c60fc6bc [Tuesday, August 29, 2023]
  - file5-NASA-news.pdf  - doc003/8e8ee6081255407da573fdd297f0719a [Tuesday, August 29, 2023]
  - file4-SK-Readme.pdf  - doc002/5f529a9d13d24f2faa9c8941f46a9169 [Tuesday, August 29, 2023]
  - file3-lorem-ipsum.docx  - doc002/75d848ced15749699b65fefc8a888400 [Tuesday, August 29, 2023]

====================================

Question: Any news from NASA about Orion?

Blake Answer: INFO NOT FOUND.

Taylor Answer: Yes, NASA has invited media to see the new test version of the Orion spacecraft and the hardware teams will use to recover the capsule and astronauts upon their return from space during the Artemis II mission. The event will take place at Naval Base San Diego and personnel involved in recovery operations from NASA, the U.S. Navy, and the U.S. Air Force will be available to speak with media. The Artemis II crew will participate in recovery testing at sea next year.
  Sources:

  - file5-NASA-news.pdf  - doc003/3cc5eefb83ac40cc80b445a1d70b71f0 [Tuesday, August 29, 2023]

====================================

Question: What is Orion?

Articles: INFO NOT FOUND

News: Orion is a spacecraft developed by NASA for crewed missions, including the Artemis II mission which will send four astronauts around the Moon to checkout systems ahead of future lunar missions. NASA has invited media to see the new test version of the Orion spacecraft and the hardware teams will use to recover the capsule and astronauts upon their return from space during the Artemis II mission.

====================================
Deleting memories derived from 9e1ecae343cb4134a7ec955625d51aa6202901120755348131790
Deleting memories derived from doc001
Deleting memories derived from img001
Deleting memories derived from doc002
Deleting memories derived from doc003
Deleting memories derived from webPage1

*/
