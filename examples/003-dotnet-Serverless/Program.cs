// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;

/* Use MemoryServerlessClient to run the default import pipeline
 * in the same process, without distributed queues.
 *
 * The pipeline might use settings in appsettings.json, but uses
 * 'InProcessPipelineOrchestrator' explicitly.
 *
 * Note: no web service required, each file is processed in this process. */

var memory = new KernelMemoryBuilder()
    .WithOpenAIDefaults(Env.Var("OPENAI_API_KEY"))
    // .FromAppSettings() => read "KernelMemory" settings from appsettings.json (if available), see https://github.com/microsoft/kernel-memory/blob/main/dotnet/Service/appsettings.json as an example
    // .WithAzureBlobsStorage(new AzureBlobsConfig {...})                                              => use Azure Blobs
    // .WithAzureCognitiveSearch(Env.Var("ACS_ENDPOINT"), Env.Var("ACS_API_KEY"))                      => use Azure Cognitive Search
    // .WithQdrant("http://127.0.0.1:6333")                                                            => use Qdrant docker
    // .WithAzureFormRecognizer(Env.Var("AZURE_COG_SVCS_ENDPOINT"), Env.Var("AZURE_COG_SVCS_API_KEY")) => use Azure Form Recognizer OCR
    .Build<MemoryServerless>();

// Use these boolean to enable/disable parts of the examples below
bool ingestion = true;
bool useImages = false; // Enable Azure Form Recognizer OCR to use this
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

====================================

Uploading text about E=mc^2
Uploading article file about Carbon
Uploading Image file with a news about a conference sponsored by Microsoft
Uploading a text file, a Word doc, and a PDF about Semantic Kernel
Uploading a PDF with a news about NASA and Orion
Uploading https://raw.githubusercontent.com/microsoft/kernel-memory/main/README.md

====================================

Question: What's E = m*c^2?

Answer: E = m*c^2 is the formula that represents the mass-energy equivalence principle in physics. It was proposed by Albert Einstein and states that the energy (E) of an object is equal to its mass (m) multiplied by the speed of light (c) squared. This equation shows that mass and energy are interchangeable and that a small amount of mass can be converted into a large amount of energy. It is a fundamental concept in understanding the relationship between mass and energy in the universe.

====================================

Question: What's Semantic Kernel?

Answer: Semantic Kernel is a lightweight SDK (Software Development Kit) that allows integration of AI Large Language Models (LLMs) with conventional programming languages. It combines natural language semantic functions, traditional code native functions, and embeddings-based memory to enhance applications with AI. SK supports prompt templating, function chaining, vectorized memory, and intelligent planning capabilities. It encapsulates several design patterns from AI research, such as prompt chaining, recursive reasoning, summarization, zero/few-shot learning, contextual memory, and more. SK is open-source and developers are invited to contribute to its development.

  Sources:

  - file4-SK-Readme.pdf  - doc002/f26cfdda742d4cfd99d614055552e11a [Tuesday, August 29, 2023]
  - content.url  - webPage1/ed0b16e24ec74dbb924a113f9a9e254a [Tuesday, August 29, 2023]
  - file3-lorem-ipsum.docx  - doc002/cac17ef53fa6423980f0961aa007ec51 [Tuesday, August 29, 2023]
  - content.txt  - c8f87691264d4f3abd0f90948b7f6021202308280656304478730/dd2ecc0d4228468c870f41dc4dfb0a27 [Tuesday, August 29, 2023]
  - content.txt  - 49bac5d2ea50432cb67b784253818e40202308280658336581710/9f97247b66f84298a643da398b1097f7 [Tuesday, August 29, 2023]

====================================

Question: Which conference is Microsoft sponsoring?

Answer: Microsoft is sponsoring the Automotive News World Congress 2023 event in Detroit.

  Sources:

  - file6-ANWC-image.jpg  - img001/efdb3a0f2aca4035b840aaa3898c1892 [Tuesday, August 29, 2023]
  - file5-NASA-news.pdf  - doc003/acc1767a64974c3480a323d488dd4acc [Tuesday, August 29, 2023]
  - file4-SK-Readme.pdf  - doc002/f26cfdda742d4cfd99d614055552e11a [Tuesday, August 29, 2023]
  - content.url  - webPage1/ed0b16e24ec74dbb924a113f9a9e254a [Tuesday, August 29, 2023]
  - file3-lorem-ipsum.docx  - doc002/cac17ef53fa6423980f0961aa007ec51 [Tuesday, August 29, 2023]

====================================

Question: Any news from NASA about Orion?

Blake Answer: INFO NOT FOUND

Taylor Answer: Yes, NASA has invited media to see the test version of the Orion spacecraft and the recovery hardware that will be used for the Artemis II mission. The event will take place at Naval Base San Diego on August 2nd. Personnel from NASA, the U.S. Navy, and the U.S. Air Force will be available for interviews. The recovery operations team is currently conducting tests in the Pacific Ocean to prepare for the Artemis II mission, which will send four astronauts around the Moon. The crew of Artemis II will participate in recovery testing at sea next year.
  Sources:

  - file5-NASA-news.pdf  - doc003/acc1767a64974c3480a323d488dd4acc [Tuesday, August 29, 2023]

====================================

Question: What is Orion?
warn: Microsoft.KernelMemory.Search.SearchClient[0]
      No memories available

Articles: INFO NOT FOUND

News: Orion is a spacecraft developed by NASA for human space exploration. It is designed to carry astronauts beyond low Earth orbit, including missions to the Moon and potentially Mars. The spacecraft is part of NASA's Artemis program, which aims to return humans to the Moon by 2024. The Artemis II mission will be the first crewed mission under the Artemis program and will send four astronauts around the Moon to test systems for future lunar missions. The recovery operations team is currently conducting tests in the Pacific Ocean to prepare for the Artemis II mission, and the crew of Artemis II will participate in recovery testing at sea next year.

====================================

Deleting memories derived from 15af4991c22d4728bd2f515e7617c5ee202901120542397729100
Deleting memories derived from doc001
Deleting memories derived from doc002
Deleting memories derived from doc003
Deleting memories derived from webPage1
*/
