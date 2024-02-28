// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI.OpenAI;

/* Use MemoryServerlessClient to run the default import pipeline
 * in the same process, without distributed queues.
 *
 * The pipeline might use settings in appsettings.json, but uses
 * 'InProcessPipelineOrchestrator' explicitly.
 *
 * Note: no web service required, each file is processed in this process. */

var openAIConfig = new OpenAIConfig();
var azureOpenAITextConfig = new AzureOpenAIConfig();
var azureOpenAIEmbeddingConfig = new AzureOpenAIConfig();
var llamaConfig = new LlamaSharpConfig();
var searchClientConfig = new SearchClientConfig();
var azDocIntelConfig = new AzureAIDocIntelConfig();

new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddJsonFile("appsettings.Development.json", optional: true)
    .Build()
    .BindSection("KernelMemory:Services:OpenAI", openAIConfig)
    .BindSection("KernelMemory:Services:LlamaSharp", llamaConfig)
    .BindSection("KernelMemory:Services:AzureOpenAIText", azureOpenAITextConfig)
    .BindSection("KernelMemory:Services:AzureOpenAIEmbedding", azureOpenAIEmbeddingConfig)
    .BindSection("KernelMemory:Services:AzureAIDocIntel", azDocIntelConfig)
    .BindSection("KernelMemory:Retrieval:SearchClient", searchClientConfig);

var memory = new KernelMemoryBuilder()
    // .WithOpenAIDefaults(Env.Var("OPENAI_API_KEY"))
    // .WithOpenAI(openAIConfig)
    // .WithLlamaTextGeneration(llamaConfig)
    .WithAzureOpenAITextGeneration(azureOpenAITextConfig, new DefaultGPTTokenizer())
    .WithAzureOpenAITextEmbeddingGeneration(azureOpenAIEmbeddingConfig, new DefaultGPTTokenizer())
    // .WithAzureAIDocIntel(azDocIntelConfig)                                         // => use Azure AI Document Intelligence OCR
    // .WithAzureBlobsStorage(new AzureBlobsConfig {...})                             // => use Azure Blobs
    // .WithAzureAISearch(Env.Var("AZSEARCH_ENDPOINT"), Env.Var("AZSEARCH_API_KEY"))  // => use Azure AI Search
    // .WithQdrant("http://127.0.0.1:6333")                                           // => use Qdrant to store memories
    // .WithSimpleFileStorage(SimpleFileStorageConfig.Persistent)
    .Build<MemoryServerless>();

// Use these boolean to enable/disable parts of the examples below
bool ingestion = true;
bool useImages = false; // Deploy Azure AI Document Intelligence to use this
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

    // Working with HTML files
    toDelete.Add("htmlDoc001");
    if (!await memory.IsDocumentReadyAsync(documentId: "htmlDoc001"))
    {
        Console.WriteLine("Uploading a HTML file about Apache Submarine project");
        await memory.ImportDocumentAsync(new Document("htmlDoc001").AddFile("file7-submarine.html").AddTag("user", "Ela"));
    }
    else
    {
        Console.WriteLine("htmlDoc001 already uploaded.");
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
// === RETRIEVAL =========
// =======================

if (retrieval)
{
    Console.WriteLine("\n====================================\n");

    // Question without filters
    var question = "What's E = m*c^2?";
    Console.WriteLine($"Question: {question}");

    var answer = await memory.AskAsync(question, minRelevance: 0.76);
    Console.WriteLine($"\nAnswer: {answer.Result}");

    Console.WriteLine("\n====================================\n");

    // Another question without filters
    question = "What's Kernel Memory?";
    Console.WriteLine($"Question: {question}");

    answer = await memory.AskAsync(question, minRelevance: 0.76);
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

        answer = await memory.AskAsync(question, minRelevance: 0.76);
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

    // Question about HTML content
    question = "What's the latest version of Apache Submarine?";
    Console.WriteLine($"Question: {question}");

    answer = await memory.AskAsync(question, filter: MemoryFilters.ByTag("user", "Ela"));
    Console.WriteLine($"\nAnswer: {answer.Result}");

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
Uploading a text file, a Word doc, and a PDF about Kernel Memory
Uploading a PDF with a news about NASA and Orion
Uploading https://raw.githubusercontent.com/microsoft/kernel-memory/main/README.md
Uploading https://raw.githubusercontent.com/microsoft/kernel-memory/main/docs/security/security-filters.md

====================================

Question: What's E = m*c^2?

Answer: E = m*c^2 is the equation that represents the principle of mass-energy equivalence, which is a fundamental concept in physics. This equation was formulated by the physicist Albert Einstein. In this formula:
- E stands for energy,
- m stands for mass, and
- c stands for the speed of light in a vacuum, which is approximately 299,792,458 meters per second.
The equation states that the energy (E) of a system in its rest frame is equal to its mass (m) multiplied by the square of the speed of light (c^2). This implies that mass and energy are interchangeable; they are different forms of the same thing. A small amount of mass can be converted into a very large amount of energy because the speed of light squared (c^2) is a very large number. This principle is a cornerstone of modern physics and has important implications in various fields, including nuclear physics, where it explains the large amounts of energy released in nuclear reactions.

====================================

Question: What's Kernel Memory?

Answer: Kernel Memory (KM) is a multi-modal AI Service designed to efficiently index datasets through custom continuous data hybrid pipelines. It supports various advanced features such as Retrieval Augmented Generation (RAG), synthetic memory, prompt engineering, and custom semantic memory processing. KM is equipped with a GPT Plugin, web clients, a .NET library for embedded applications, and is available as a Docker container.
The service utilizes advanced embeddings and Large Language Models (LLMs) to enable natural language querying, allowing users to obtain answers from indexed data, complete with citations and links to the original sources. KM is designed for seamless integration with other tools and services, such as Semantic Kernel, Microsoft Copilot, and ChatGPT, enhancing data-driven features in applications built for popular AI platforms.
Kernel Memory is built upon the feedback and lessons learned from the development of Semantic Kernel (SK) and Semantic Memory (SM). It offers several features that simplify tasks such as storing files, extracting text from files, securing user data, and more. The KM codebase is entirely in .NET, which allows it to be used from any language, tool, or platform, including browser extensions and ChatGPT assistants.
KM supports a wide range of data formats, including web pages, PDFs, images, MS Office documents, Markdown, text, and JSON files. It also integrates with various AI services like Azure OpenAI, OpenAI, and LLama, and supports vector storage solutions such as Azure AI Search, Postgres

 Sources:

 - file4-KM-Readme.pdf  - default/doc002/d2fdf058c00946448d0166879da28a49 [Tuesday, February 27, 2024]
 - https://raw.githubusercontent.com/microsoft/kernel-memory/main/README.md [Tuesday, February 27, 2024]
 - https://raw.githubusercontent.com/microsoft/kernel-memory/main/docs/security/security-filters.md [Tuesday, February 27, 2024]

====================================

Question: Any news from NASA about Orion?

Blake Answer (none expected): INFO NOT FOUND

Taylor Answer: Yes, there is news from NASA regarding the Orion spacecraft. NASA has invited media to view the new test version of the Orion spacecraft and the hardware that will be used to recover the capsule and astronauts upon their return from space during the Artemis II mission. This event is scheduled to take place at 11 a.m. PDT on Wednesday, August 2, at Naval Base San Diego.
NASA and Department of Defense personnel have been practicing recovery operations aboard the USS John P. Murtha using a crew module test article to verify that the recovery team will be ready to recover the Artemis II crew and the Orion spacecraft. These operations are part of a series of tests in the Pacific Ocean to demonstrate and evaluate the processes, procedures, and hardware for recovery operations for crewed Artemis missions.
The Artemis II mission will be NASA's first crewed mission under the Artemis program, which will send four astronauts in Orion around the Moon to check out systems ahead of future lunar missions. The Artemis II crew consists of NASA astronauts Reid Wiseman, Victor Glover, and Christina Koch, along with CSA (Canadian Space Agency) astronaut Jeremy Hansen. They will participate in recovery testing at sea next year.
For more information about the Artemis program, NASA has provided a link: https://www.nasa.gov/artemis.
 Sources:

 - file5-NASA-news.pdf  - default/doc003/2255254b21a3497180209b2705d3953e [Tuesday, February 27, 2024]

====================================

Question: What's the latest version of Apache Submarine?

Answer: The latest version of Apache Submarine is 0.8.0, released on 2023-09-23.

====================================


Question: What is Orion?

Articles (none expected): INFO NOT FOUND
warn: Microsoft.KernelMemory.Search.SearchClient[0]
     No memories available

News: Orion is NASA's spacecraft designed for deep space exploration, including missions to the Moon and potentially Mars in the future. It is part of NASA's Artemis program, which aims to return humans to the Moon and establish a sustainable presence there as a stepping stone for further exploration. The Orion spacecraft is built to carry astronauts beyond low Earth orbit, equipped with life support, propulsion, thermal protection, and avionics systems necessary for extended missions in deep space. It is intended to be used for the Artemis II mission, which will be the first crewed mission of the Artemis program, sending four astronauts around the Moon to test the spacecraft's systems.
====================================
Deleting memories derived from d2cd890772d34946bcb8f04caf20dc40202402270112081947080
Deleting memories derived from doc001
Deleting memories derived from doc002
Deleting memories derived from doc003
Deleting memories derived from webPage1
Deleting memories derived from webPage2

*/
