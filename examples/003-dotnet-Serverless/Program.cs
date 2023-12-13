// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI.Llama;
using Microsoft.KernelMemory.AI.Tokenizers;
using Microsoft.KernelMemory.DataFormats.Image.AzureAIDocIntel;

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
    // .WithOpenAI(openAICfg)
    // .WithLlamaTextGeneration(llamaConfig)
    .WithAzureOpenAITextGeneration(azureOpenAITextConfig, new DefaultGPTTokenizer())
    .WithAzureOpenAITextEmbeddingGeneration(azureOpenAIEmbeddingConfig, new DefaultGPTTokenizer())
    // .WithAzureAIDocIntel(azDocIntelConfig)                                         // => use Azure AI Document Intelligence OCR
    // .WithAzureBlobsStorage(new AzureBlobsConfig {...})                             // => use Azure Blobs
    // .WithAzureAISearch(Env.Var("AZSEARCH_ENDPOINT"), Env.Var("AZSEARCH_API_KEY"))  // => use Azure AI Search
    // .WithQdrant("http://127.0.0.1:6333")                                           // => use Qdrant to store memories
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

    var answer = await memory.AskAsync(question, minRelevance: 0.76);
    Console.WriteLine($"\nAnswer: {answer.Result}");

    Console.WriteLine("\n====================================\n");

    // Another question without filters
    question = "What's Semantic Kernel?";
    Console.WriteLine($"Question: {question}");

    answer = await memory.AskAsync(question, minRelevance: 0.76);
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

        answer = await memory.AskAsync(question, minRelevance: 0.76);
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
    Console.WriteLine($"\nBlake Answer (none expected): {answer.Result}");

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
Uploading https://raw.githubusercontent.com/microsoft/kernel-memory/main/docs/SECURITY_FILTERS.md

====================================

Question: What's E = m*c^2?

Answer: E = m*c^2 is a formula in physics that describes the mass–energy equivalence. This principle, proposed by Albert Einstein, states that the energy of an object (E) is equal to the mass (m) of that object times the speed of light (c) squared. This relationship is observed in a system's rest frame, where mass and energy differ only by a multiplicative constant and the units of measurement.

====================================

Question: What's Semantic Kernel?

Answer: Semantic Kernel (SK) is a lightweight Software Development Kit (SDK) that enables the integration of AI Large Language Models (LLMs) with conventional programming languages. It combines natural language semantic functions, traditional code native functions, and embeddings-based memory to unlock new potential and add value to applications with AI.

SK supports prompt templating, function chaining, vectorized memory, and intelligent planning capabilities. It encapsulates several design patterns from the latest AI research, allowing developers to infuse their applications with plugins like prompt chaining, recursive reasoning, summarization, zero/few-shot learning, contextual memory, long-term memory, embeddings, semantic indexing, planning, retrieval-augmented generation, and accessing external knowledge stores as well as your own data.

Semantic Kernel is available for use with C# and Python and can be explored and used to build AI-first apps. It is an open-source project, inviting developers to contribute and join in its development.

Sources:

- file4-SK-Readme.pdf  - doc002/a166fd04b91a44cd919a300e84931bdf [Friday, December 8, 2023]
- content.url  - webPage1/fbcb60da9d5a4ba1a390e108941fc7ad [Friday, December 8, 2023]
- content.url  - webPage2/79a67b4f470b43549fce1b9a3de21c95 [Friday, December 8, 2023]

====================================

Question: Which conference is Microsoft sponsoring?

Answer: Microsoft is sponsoring the Automotive News World Congress 2023 event, which is taking place in Detroit, Michigan on September 12, 2023.

Sources:

- file6-ANWC-image.jpg  - img001/ac7d8bc0051945a689aa23d1fa9092b2 [Friday, December 8, 2023]
- file5-NASA-news.pdf  - doc003/be2411fdc3e84c5995a7753beb927ecd [Friday, December 8, 2023]
- content.url  - webPage1/fbcb60da9d5a4ba1a390e108941fc7ad [Friday, December 8, 2023]
- file4-SK-Readme.pdf  - doc002/a166fd04b91a44cd919a300e84931bdf [Friday, December 8, 2023]
- file3-lorem-ipsum.docx  - doc002/a1269887842d4748980cbdd7e1aabc12 [Friday, December 8, 2023]

====================================

Question: Any news from NASA about Orion?

Blake Answer (none expected): INFO NOT FOUND

Taylor Answer: Yes, NASA has invited media to see the new test version of the Orion spacecraft and the hardware teams will use to recover the capsule and astronauts upon their return from space during the Artemis II mission. The event is scheduled to take place at 11 a.m. PDT on Wednesday, Aug. 2, at Naval Base San Diego. Teams are currently conducting the first in a series of tests in the Pacific Ocean to demonstrate and evaluate the processes, procedures, and hardware for recovery operations for crewed Artemis missions. The tests will help prepare the team for Artemis II, NASA’s first crewed mission under Artemis that will send four astronauts in Orion around the Moon to checkout systems ahead of future lunar missions. The Artemis II crew – NASA astronauts Reid Wiseman, Victor Glover, and Christina Koch, and CSA (Canadian Space Agency) astronaut Jeremy Hansen – will participate in recovery testing at sea next year.
Sources:

- file5-NASA-news.pdf  - doc003/be2411fdc3e84c5995a7753beb927ecd [Friday, December 8, 2023]

====================================

Question: What is Orion?

Articles (none expected): INFO NOT FOUND

News: Orion is a spacecraft developed by NASA. It is being used in the Artemis II mission, which is NASA's first crewed mission under the Artemis program. The mission will send four astronauts in the Orion spacecraft around the Moon to check out systems ahead of future lunar missions.
====================================
Deleting memories derived from d421ecd8e79747ec8ed5f1db49baba2c202312070451357022920
Deleting memories derived from doc001
Deleting memories derived from img001
Deleting memories derived from doc002
Deleting memories derived from doc003
Deleting memories derived from webPage1
Deleting memories derived from webPage2
*/
