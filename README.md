# Semantic Memory

**Semantic Memory** is an open-source library and [service](dotnet/Service)
specialized in the efficient indexing of datasets through custom continuous data
pipelines.

![image](https://github.com/microsoft/semantic-memory/assets/371009/31894afa-d19e-4e9b-8d0f-cb889bf5c77f)

Utilizing advanced embeddings and LLMs, the system enables Natural Language
querying for obtaining answers from the indexed data, complete with citations
and links to the original sources.

![image](https://github.com/microsoft/semantic-memory/assets/371009/c5f0f6c3-814f-45bf-b055-063f23ed80ea)

Designed for seamless integration with
[Semantic Kernel](https://github.com/microsoft/semantic-kernel),
Semantic Memory enhances data-driven features in applications built using SK.

> ℹ️ **NOTE**: the documentation below is work in progress, will evolve quickly
> as is not fully functional yet.

# Semantic Memory in serverless mode

Semantic Memory works and scales at best when running as a service, allowing to
ingest thousands of documents and information without blocking your app.

However, you can use Semantic Memory also serverless, embedding the `MemoryServerlessClient`
in your app.

> ### Importing documents into your Semantic Memory can be as simple as this:
>
> ```csharp
> var memory = new MemoryServerlessClient();
>
> // Import a file (default user)
> await memory.ImportFileAsync("meeting-transcript.docx");
>
> // Import a file specifying a User and Tags
> await memory.ImportFileAsync("business-plan.docx",
>     new DocumentDetails("user@some.email", "file1")
>         .AddTag("collection", "business")
>         .AddTag("collection", "plans")
>         .AddTag("type", "doc"));
> ```

> ### Asking questions:
>
> ```csharp
> var answer1 = await memory.AskAsync("How many people attended the meeting?");
>
> var answer2 = await memory.AskAsync("user@some.email", "what's the project timeline?");
> ```

The code leverages the default documents ingestion pipeline:

1. Extract text: recognize the file format and extract the information
2. Partition the text in small chunks, to optimize search
3. Extract embedding using an LLM embedding generator
4. Save embedding into a vector index such as
   [Azure Cognitive Search](https://learn.microsoft.com/en-us/azure/search/vector-search-overview),
   [Qdrant](https://qdrant.tech/) or other DBs.

Documents are organized by users, safeguarding their private information.
Furthermore, memories can be categorized and structured using **tags**, enabling
efficient search and retrieval through faceted navigation.

# Data lineage, citations

All memories and answers are fully correlated to the data provided. When
producing an answer, Semantic Memory includes all the information needed
to verify its accuracy:

```csharp
await memory.ImportFileAsync("NASA-news.pdf");

var answer = await memory.AskAsync("Any news from NASA about Orion?");

Console.WriteLine(answer.Result + "/n");

foreach (var x in answer.RelevantSources)
{
    Console.WriteLine($"  * {x.SourceName} -- {x.Partitions.First().LastUpdate:D}");
}
```

> Yes, there is news from NASA about the Orion spacecraft. NASA has invited the
> media to see a new test version of the Orion spacecraft and the hardware that
> will be used to recover the capsule and astronauts upon their return from
> space during the Artemis II mission. The event is scheduled to take place at
> Naval Base San Diego on Wednesday, August 2, at 11 a.m. PDT. Personnel from
> NASA, the U.S. Navy, and the U.S. Air Force will be available to speak with
> the media. Teams are currently conducting tests in the Pacific Ocean to
> demonstrate and evaluate the processes, procedures, and hardware for recovery
> operations for crewed Artemis missions. These tests will help prepare the
> team for Artemis II, which will be NASA's first crewed mission under the
> Artemis program. The Artemis II crew, consisting of NASA astronauts Reid
> Wiseman, Victor Glover, and Christina Koch, and Canadian Space Agency
> astronaut Jeremy Hansen, will participate in recovery testing at sea next
> year. For more information about the Artemis program, you can visit the NASA
> website.
>
> - **NASA-news.pdf -- Tuesday, August 1, 2023**

## Using Semantic Memory Service

Depending on your scenarios, you might want to run all the code **locally
inside your process, or remotely through an asynchronous service.**

If you're importing small files, and need only C# or only Python, and can block
the process during the import, local-in-process execution can be fine, using
the **MemoryServerlessClient** seen above.

However, if you are in one of these scenarios:

* I'd just like a web service to import data and send queries to answer
* My app is written in **TypeScript, Java, Rust, or some other language**
* I want to define **custom pipelines mixing multiple languages**
  like Python, TypeScript, etc
* I'm importing **big documents that can require minutes to process**, and
  I don't want to block the user interface
* I need memory import to **run independently, supporting failures and retry
  logic**

then you can deploy Semantic Memory as a service, plugging in the
default handlers or your custom Python/TypeScript/Java/etc. handlers,
and leveraging the asynchronous non-blocking memory encoding process,
sending documents and asking questions using the **MemoryWebClient**.

[Here](dotnet/Service/README.md) you can find a complete set of instruction
about [how to run the Semantic Memory service](dotnet/Service/README.md).

If you want to give the service a quick test, use the following command
to **start the Semantic Memory Service**:

> ### On WSL / Linux / MacOS:
>
> ```shell
> cd dotnet/Service
> ./setup.sh
> ./run.sh
> ```

> ### On Windows:
>
> ```shell
> cd dotnet/Service
> setup.cmd
> run.cmd
> ```

> ### To import files using Semantic Memory **web service**, use `MemoryWebClient`:
>
> ```csharp
> #reference dotnet/ClientLib/ClientLib.csproj
>
> var memory = new MemoryWebClient("http://127.0.0.1:9001"); // <== URL where the web service is running
>
> await memory.ImportFileAsync("meeting-transcript.docx");
>
> await memory.ImportFileAsync("business-plan.docx",
>     new DocumentDetails("file1", "user0022")
>         .AddTag("collection", "business")
>         .AddTag("collection", "plans")
>         .AddTag("type", "doc"));
> ```

> ### Getting answers via the web service
> ```
> curl http://127.0.0.1:9001/ask -d'{"query":"Any news from NASA about Orion?"}' -H 'Content-Type: application/json'
> ```
> ```json
> {
>   "Query": "Any news from NASA about Orion?",
>   "Text": "Yes, there is news from NASA about the Orion spacecraft. NASA has invited the media to see a new test version of the Orion spacecraft and the hardware that will be used to recover the capsule and astronauts upon their return from space during the Artemis II mission. The event is scheduled to take place at Naval Base San Diego on August 2nd at 11 a.m. PDT. Personnel from NASA, the U.S. Navy, and the U.S. Air Force will be available to speak with the media. Teams are currently conducting tests in the Pacific Ocean to demonstrate and evaluate the processes, procedures, and hardware for recovery operations for crewed Artemis missions. These tests will help prepare the team for Artemis II, which will be NASA's first crewed mission under the Artemis program. The Artemis II crew, consisting of NASA astronauts Reid Wiseman, Victor Glover, and Christina Koch, and Canadian Space Agency astronaut Jeremy Hansen, will participate in recovery testing at sea next year. For more information about the Artemis program, you can visit the NASA website.",
>   "RelevantSources": [
>     {
>       "Link": "...",
>       "SourceContentType": "application/pdf",
>       "SourceName": "file5-NASA-news.pdf",
>       "Partitions": [
>         {
>           "Text": "Skip to main content\nJul 28, 2023\nMEDIA ADVISORY M23-095\nNASA Invites Media to See Recovery Craft for\nArtemis Moon Mission\n(/sites/default/ﬁles/thumbnails/image/ksc-20230725-ph-fmx01_0003orig.jpg)\nAboard the USS John P. Murtha, NASA and Department of Defense personnel practice recovery operations for Artemis II in July. A\ncrew module test article is used to help verify the recovery team will be ready to recovery the Artemis II crew and the Orion spacecraft.\nCredits: NASA/Frank Michaux\nMedia are invited to see the new test version of NASA’s Orion spacecraft and the hardware teams will use\nto recover the capsule and astronauts upon their return from space during the Artemis II\n(http://www.nasa.gov/artemis-ii) mission. The event will take place at 11 a.m. PDT on Wednesday, Aug. 2,\nat Naval Base San Diego.\nPersonnel involved in recovery operations from NASA, the U.S. Navy, and the U.S. Air Force will be\navailable to speak with media.\nU.S. media interested in attending must RSVP by 4 p.m., Monday, July 31, to the Naval Base San Diego\nPublic Aﬀairs (mailto:nbsd.pao@us.navy.mil) or 619-556-7359.\nOrion Spacecraft (/exploration/systems/orion/index.html)\nNASA Invites Media to See Recovery Craft for Artemis Moon Miss... https://www.nasa.gov/press-release/nasa-invites-media-to-see-recov...\n1 of 3 7/28/23, 4:51 PMTeams are currently conducting the ﬁrst in a series of tests in the Paciﬁc Ocean to demonstrate and\nevaluate the processes, procedures, and hardware for recovery operations (https://www.nasa.gov\n/exploration/systems/ground/index.html) for crewed Artemis missions. The tests will help prepare the\nteam for Artemis II, NASA’s ﬁrst crewed mission under Artemis that will send four astronauts in Orion\naround the Moon to checkout systems ahead of future lunar missions.\nThe Artemis II crew – NASA astronauts Reid Wiseman, Victor Glover, and Christina Koch, and CSA\n(Canadian Space Agency) astronaut Jeremy Hansen – will participate in recovery testing at sea next year.\nFor more information about Artemis, visit:\nhttps://www.nasa.gov/artemis (https://www.nasa.gov/artemis)\n-end-\nRachel Kraft\nHeadquarters, Washington\n202-358-1100\nrachel.h.kraft@nasa.gov (mailto:rachel.h.kraft@nasa.gov)\nMadison Tuttle\nKennedy Space Center, Florida\n321-298-5868\nmadison.e.tuttle@nasa.gov (mailto:madison.e.tuttle@nasa.gov)\nLast Updated: Jul 28, 2023\nEditor: Claire O’Shea\nTags:  Artemis (/artemisprogram),Ground Systems (http://www.nasa.gov/exploration/systems/ground\n/index.html),Kennedy Space Center (/centers/kennedy/home/index.html),Moon to Mars (/topics/moon-to-\nmars/),Orion Spacecraft (/exploration/systems/orion/index.html)\nNASA Invites Media to See Recovery Craft for Artemis Moon Miss... https://www.nasa.gov/press-release/nasa-invites-media-to-see-recov...\n2 of 3 7/28/23, 4:51 PM",
>           "Relevance": 0.8430657,
>           "SizeInTokens": 863,
>           "LastUpdate": "2023-08-01T08:15:02-07:00"
>         }
>       ]
>     }
>   ]
> }
> ```

You can find a [full example here](samples/dotnet-WebClient/).

## Custom memory ingestion pipelines

On the other hand, if you need a custom data pipeline, you can also
customize the steps, which will be handled by your custom business logic:

```csharp
var app = AppBuilder.Build();
var storage = app.Services.GetService<IContentStorage>();

// Use a local, synchronous, orchestrator
var orchestrator = new InProcessPipelineOrchestrator(storage);

// Define custom .NET handlers
var step1 = new MyHandler1("step1", orchestrator);
var step2 = new MyHandler2("step2", orchestrator);
var step3 = new MyHandler3("step3", orchestrator);
await orchestrator.AddHandlerAsync(step1);
await orchestrator.AddHandlerAsync(step2);
await orchestrator.AddHandlerAsync(step3);

// Instantiate a custom pipeline
var pipeline = orchestrator
    .PrepareNewFileUploadPipeline("mytest", "user-id-1", new[] { "memory-collection" })
    .AddUploadFile("file1", "file1.docx", "file1.docx")
    .AddUploadFile("file2", "file2.pdf", "file2.pdf")
    .Then("step1")
    .Then("step2")
    .Then("step3")
    .Build();

// Execute in process, process all files with all the handlers
await orchestrator.RunPipelineAsync(pipeline);
```

# Examples and Tools

1. [Using the web service](samples/dotnet-WebClient)
2. [Importing files without the service (serverless ingestion)](samples/dotnet-Serverless)
3. [Upload files and get answers from command line with curl](samples/curl)
4. [Writing a custom pipeline handler](samples/dotnet-CustomHandler)
5. [Importing files with custom steps](samples/dotnet-ServerlessCustomPipeline)
6. [Extracting text from documents](samples/dotnet-ExtractTextFromDocs)
7. [Curl script to upload files](tools/upload-file.sh)
8. [Script to start RabbitMQ for development tasks](tools/run-rabbitmq.sh)
