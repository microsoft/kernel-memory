// Copyright (c) Microsoft. All rights reserved.

// ReSharper disable InconsistentNaming
// ReSharper disable CommentTypo

#pragma warning disable CS8602 // memory is initialized before usage
#pragma warning disable CS0162 // unreachable code is managed via boolean settings

using Microsoft.KernelMemory;

/* Use MemoryWebClient to run the default import pipeline
 * deployed as a web service at "http://127.0.0.1:9001/".
 *
 * Note: start the Kernel Memory service before running this.
 * Note: if the web service uses distributed handlers, make sure
 *       handlers are running to get the pipeline to complete,
 *       otherwise the web service might just upload the files
 *       without extracting memories. */
public static class Program
{
    private static MemoryWebClient? s_memory;
    private static readonly List<string> s_toDelete = new();

    // Change this to True and configure Azure Document Intelligence to test OCR and support for images
    private const bool ImageSupportDemoEnabled = true;

    public static async Task Main()
    {
        s_memory = new MemoryWebClient("http://127.0.0.1:9001/");

        // =======================
        // === INGESTION =========
        // =======================

        await StoreText();
        await StoreFile();
        await StoreMultipleFiles();
        await StoreFileWithMultipleTags();
        await StoreWebPage();
        await StoreHTMLFile();
        await StoreWithCustomPipeline();
        await StoreImage();
        await StoreExcel();
        await StoreJson();

        // Wait for remote ingestion pipelines to complete
        foreach (var docId in s_toDelete)
        {
            while (!await s_memory.IsDocumentReadyAsync(documentId: docId))
            {
                Console.WriteLine("Waiting for memory ingestion to complete...");
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }

        // =======================
        // === RETRIEVAL =========
        // =======================

        await AskSimpleQuestion();
        await AskSimpleQuestionAndShowSources();
        await AskQuestionAboutImageContent();
        await AskQuestionUsingFilter();
        await AskQuestionsFilteringByUser();
        await AskQuestionsFilteringByTypeTag();
        await AskQuestionsAboutExcelData();
        await AskQuestionsAboutJsonFile();
        await DownloadFile();

        // =======================
        // === PURGE =============
        // =======================

        await DeleteMemories();
    }

    // =======================
    // === INGESTION =========
    // =======================

    // Uploading some text, without using files. Hold a copy of the ID to delete it later.
    private static async Task StoreText()
    {
        Console.WriteLine("Uploading text about E=mc^2");
        var docId = await s_memory.ImportTextAsync("In physics, mass–energy equivalence is the relationship between mass and energy " +
                                                   "in a system's rest frame, where the two quantities differ only by a multiplicative " +
                                                   "constant and the units of measurement. The principle is described by the physicist " +
                                                   "Albert Einstein's formula: E = m*c^2");
        Console.WriteLine($"- Document Id: {docId}");
        s_toDelete.Add(docId);
    }

    // Simple file upload, with document ID
    private static async Task StoreFile()
    {
        Console.WriteLine("Uploading article file about Carbon");
        var docId = await s_memory.ImportDocumentAsync("file1-Wikipedia-Carbon.txt", documentId: "doc001");
        s_toDelete.Add(docId);
        Console.WriteLine($"- Document Id: {docId}");
    }

    // Extract memory from images (OCR required)
    private static async Task StoreImage()
    {
        if (!ImageSupportDemoEnabled) { return; }

        Console.WriteLine("Uploading Image file with a news about a conference sponsored by Microsoft");
        var docId = await s_memory.ImportDocumentAsync(new Document("img001").AddFiles(["file6-ANWC-image.jpg"]));
        s_toDelete.Add(docId);
        Console.WriteLine($"- Document Id: {docId}");
    }

    // Uploading multiple files and adding a user tag, checking if the document already exists
    private static async Task StoreMultipleFiles()
    {
        if (!await s_memory.IsDocumentReadyAsync(documentId: "doc002"))
        {
            Console.WriteLine("Uploading a text file, a Word doc, and a PDF about Kernel Memory");
            var docId = await s_memory.ImportDocumentAsync(new Document("doc002")
                .AddFiles(["file2-Wikipedia-Moon.txt", "file3-lorem-ipsum.docx", "file4-KM-Readme.pdf"])
                .AddTag("user", "Blake"));
            s_toDelete.Add(docId);
            Console.WriteLine($"- Document Id: {docId}");
        }
        else
        {
            Console.WriteLine("doc002 already uploaded.");
        }

        s_toDelete.Add("doc002");
    }

    // Categorizing files with several tags
    private static async Task StoreFileWithMultipleTags()
    {
        if (!await s_memory.IsDocumentReadyAsync(documentId: "doc003"))
        {
            Console.WriteLine("Uploading a PDF with a news about NASA and Orion");
            var docId = await s_memory.ImportDocumentAsync(new Document("doc003")
                .AddFile("file5-NASA-news.pdf")
                .AddTag("user", "Taylor")
                .AddTag("collection", "meetings")
                .AddTag("collection", "NASA")
                .AddTag("collection", "space")
                .AddTag("type", "news"));
            s_toDelete.Add(docId);
            Console.WriteLine($"- Document Id: {docId}");
        }
        else
        {
            Console.WriteLine("doc003 already uploaded.");
        }

        s_toDelete.Add("doc003");
    }

    // Downloading web pages
    private static async Task StoreWebPage()
    {
        if (!await s_memory.IsDocumentReadyAsync("webPage1"))
        {
            Console.WriteLine("Uploading https://raw.githubusercontent.com/microsoft/kernel-memory/main/README.md");
            var docId = await s_memory.ImportWebPageAsync("https://raw.githubusercontent.com/microsoft/kernel-memory/main/README.md", documentId: "webPage1");
            s_toDelete.Add(docId);
            Console.WriteLine($"- Document Id: {docId}");
        }
        else
        {
            Console.WriteLine("webPage1 already uploaded.");
        }

        s_toDelete.Add("webPage1");
    }

    // Working with HTML files
    private static async Task StoreHTMLFile()
    {
        if (!await s_memory.IsDocumentReadyAsync(documentId: "htmlDoc001"))
        {
            Console.WriteLine("Uploading a HTML file about Apache Submarine project");
            var docId = await s_memory.ImportDocumentAsync(new Document("htmlDoc001").AddFile("file7-submarine.html").AddTag("user", "Ela"));
            s_toDelete.Add(docId);
            Console.WriteLine($"- Document Id: {docId}");
        }
        else
        {
            Console.WriteLine("htmlDoc001 already uploaded.");
        }

        s_toDelete.Add("htmlDoc001");
    }

    // Custom pipeline steps
    private static async Task StoreWithCustomPipeline()
    {
        if (!await s_memory.IsDocumentReadyAsync("webPage2"))
        {
            Console.WriteLine("Uploading https://raw.githubusercontent.com/microsoft/kernel-memory/main/docs/security/security-filters.md");
            var docId = await s_memory.ImportWebPageAsync("https://raw.githubusercontent.com/microsoft/kernel-memory/main/docs/security/security-filters.md",
                documentId: "webPage2",
                steps: Constants.PipelineWithoutSummary);
            s_toDelete.Add(docId);
            Console.WriteLine($"- Document Id: {docId}");
        }
        else
        {
            Console.WriteLine("webPage2 already uploaded.");
        }

        s_toDelete.Add("webPage2");
    }

    // Extract memory from Excel file
    private static async Task StoreExcel()
    {
        if (!await s_memory.IsDocumentReadyAsync(documentId: "xls01"))
        {
            Console.WriteLine("Uploading Excel file with some empty cells");
            var docId = await s_memory.ImportDocumentAsync(new Document("xls01").AddFiles(["file8-data.xlsx"]));
            s_toDelete.Add(docId);
            Console.WriteLine($"- Document Id: {docId}");
        }
        else
        {
            Console.WriteLine("xls01 already uploaded.");
        }

        s_toDelete.Add("xls01");
    }

    // Extract memory from JSON file
    private static async Task StoreJson()
    {
        if (!await s_memory.IsDocumentReadyAsync(documentId: "json01"))
        {
            Console.WriteLine("Uploading JSON file");
            var docId = await s_memory.ImportDocumentAsync(new Document("json01").AddFiles(["file9-settings.json"]));
            s_toDelete.Add(docId);
            Console.WriteLine($"- Document Id: {docId}");
        }
        else
        {
            Console.WriteLine("json01 already uploaded.");
        }

        s_toDelete.Add("json01");
    }

    // =======================
    // === RETRIEVAL =========
    // =======================

    // Question without filters
    private static async Task AskSimpleQuestion()
    {
        var question = "What's E = m*c^2?";
        Console.WriteLine($"Question: {question}");

        var answer = await s_memory.AskAsync(question, minRelevance: 0.76);
        Console.WriteLine($"\nAnswer: {answer.Result}");

        Console.WriteLine("\n====================================\n");

        /* OUTPUT

        Question: What's E = m*c^2?

        Answer: E = m*c^2 is the formula representing the principle of mass-energy equivalence, which was introduced by Albert Einstein. In this equation,
        E stands for energy, m represents mass, and c is the speed of light in a vacuum, which is approximately 299,792,458 meters per second (m/s).
        The equation states that the energy (E) of a system in its rest frame is equal to its mass (m) multiplied by the square of the speed of light (c^2).
        This implies that mass and energy are interchangeable; a small amount of mass can be converted into a large amount of energy and vice versa,
        due to the speed of light being a very large number when squared. This concept is a fundamental principle in physics and has important implications
        in various fields, including nuclear physics and cosmology.

        */
    }

    // Another question without filters and show sources
    private static async Task AskSimpleQuestionAndShowSources()
    {
        var question = "What's Kernel Memory?";
        Console.WriteLine($"Question: {question}");

        var answer = await s_memory.AskAsync(question, minRelevance: 0);
        Console.WriteLine($"\nAnswer: {answer.Result}\n\n  Sources:\n");

        // Show sources / citations
        foreach (var x in answer.RelevantSources)
        {
            Console.WriteLine(x.SourceUrl != null
                ? $"  - {x.SourceUrl} [{x.Partitions.First().LastUpdate:D}]"
                : $"  - {x.SourceName}  - {x.Link} [{x.Partitions.First().LastUpdate:D}]");
        }

        Console.WriteLine("\n====================================\n");

        /* OUTPUT

        Question: What's Kernel Memory?

        Answer: Kernel Memory (KM) is a multi-modal AI Service designed to efficiently index datasets through custom continuous data hybrid pipelines. It
        supports various advanced features such as Retrieval Augmented Generation (RAG), synthetic memory, prompt engineering, and custom semantic memory
        processing. KM is equipped with a GPT Plugin, web clients, a .NET library for embedded applications, and is available as a Docker container.

        The service utilizes advanced embeddings and Large Language Models (LLMs) to enable natural language querying, allowing users to obtain answers from
        indexed data, complete with citations and links to the original sources. KM is designed for seamless integration with other tools and services, such
        as Semantic Kernel, Microsoft Copilot, and ChatGPT, enhancing data-driven features in applications built for popular AI platforms.

        KM is built upon the feedback and lessons learned from the development of Semantic Kernel (SK) and Semantic Memory (SM). It provides several features
        that would otherwise require manual development, such as file storage, text extraction from files, a framework to secure user data, and more. The KM
        codebase is entirely in .NET, which simplifies maintenance and eliminates the need for multi-language support. As a service, KM can be utilized from
        any language, tool, or platform, including browser extensions and ChatGPT assistants.

        Kernel Memory supports a wide range of data formats, including web pages, PDFs, images, Word, PowerPoint, Excel, Markdown, text, JSON, and more.
        It offers various search capabilities, such

        Sources:

        - file4-KM-Readme.pdf  - default/doc002/58632e8b703d4a53a630f788d7cfbfb2 [Monday, April 15, 2024]
        - https://raw.githubusercontent.com/microsoft/kernel-memory/main/README.md [Monday, April 15, 2024]
        - https://raw.githubusercontent.com/microsoft/kernel-memory/main/docs/security/security-filters.md [Monday, April 15, 2024]

        */
    }

    // Ask about image content
    private static async Task AskQuestionAboutImageContent()
    {
        var question = "Which conference is Microsoft sponsoring?";
        Console.WriteLine($"Question: {question}");

        var answer = await s_memory.AskAsync(question, minRelevance: 0.76);

        Console.WriteLine(ImageSupportDemoEnabled
            ? $"\nAnswer: {answer.Result}\n\n  Sources:\n"
            : $"\nAnswer (none expected): {answer.Result}\n\n  Sources:\n");

        // Show sources / citations
        foreach (var x in answer.RelevantSources)
        {
            Console.WriteLine(x.SourceUrl != null
                ? $"  - {x.SourceUrl} [{x.Partitions.First().LastUpdate:D}]"
                : $"  - {x.SourceName}  - {x.Link} [{x.Partitions.First().LastUpdate:D}]");
        }

        Console.WriteLine("\n====================================\n");

        /* OUTPUT

        Question: Which conference is Microsoft sponsoring?

        Answer: Microsoft is sponsoring the Automotive News World Congress 2023 event.

        Sources:

        - file6-ANWC-image.jpg  - default/img001/147b84bf6ca04c17b257da14068f4426 [Monday, April 15, 2024]

        */
    }

    // Question about HTML content using a filter
    private static async Task AskQuestionUsingFilter()
    {
        var question = "What's the latest version of Apache Submarine?";
        Console.WriteLine($"Question: {question}");

        var answer = await s_memory.AskAsync(question, filter: MemoryFilters.ByTag("user", "Ela"));
        Console.WriteLine($"\nAnswer: {answer.Result}");

        Console.WriteLine("\n====================================\n");

        /* OUTPUT

        Question: What's the latest version of Apache Submarine?

        Answer: The latest version of Apache Submarine is 0.8.0, released on 2023-09-23.

        */
    }

    private static async Task AskQuestionsFilteringByUser()
    {
        // Filter question by "user" tag
        var question = "Any news from NASA about Orion?";
        Console.WriteLine($"Question: {question}");

        // Blake doesn't know
        var answer = await s_memory.AskAsync(question, filter: MemoryFilters.ByTag("user", "Blake"));
        Console.WriteLine($"\nBlake Answer (none expected): {answer.Result}");

        // Taylor knows
        answer = await s_memory.AskAsync(question, filter: MemoryFilters.ByTag("user", "Taylor"));
        Console.WriteLine($"\nTaylor Answer: {answer.Result}\n  Sources:\n");

        // Show sources / citations
        foreach (var x in answer.RelevantSources)
        {
            Console.WriteLine(x.SourceUrl != null
                ? $"  - {x.SourceUrl} [{x.Partitions.First().LastUpdate:D}]"
                : $"  - {x.SourceName}  - {x.Link} [{x.Partitions.First().LastUpdate:D}]");
        }

        Console.WriteLine("\n====================================\n");

        /* OUTPUT

        Question: Any news from NASA about Orion?

        Blake Answer (none expected): INFO NOT FOUND

        Taylor Answer: Yes, there is news from NASA regarding the Orion spacecraft. NASA has invited media to view the new test version of the Orion spacecraft
            and the hardware that will be used to recover the capsule and astronauts upon their return from space during the Artemis II mission. The event is
            scheduled to take place at 11 a.m. PDT on Wednesday, August 2, at Naval Base San Diego.

            NASA, along with the U.S. Navy and the U.S. Air Force, is currently conducting the first in a series of tests in the Pacific Ocean to demonstrate and
            evaluate the processes, procedures, and hardware for recovery operations for crewed Artemis missions. These tests are crucial for preparing the team
            for Artemis II, which will be NASA’s first crewed mission under the Artemis program. Artemis II aims to send four astronauts in the Orion spacecraft
            around the Moon to check out systems ahead of future lunar missions.

            The crew for Artemis II includes NASA astronauts Reid Wiseman, Victor Glover, and Christina Koch, as well as CSA (Canadian Space Agency) astronaut
            Jeremy Hansen. They will participate in recovery testing at sea next year.

            For more information about the Artemis program, NASA has provided a link: https://www.nasa.gov/artemis.
            Sources:

            - file5-NASA-news.pdf  - default/doc003/c89843898c094c88a7004879805fcd63 [Monday, April 15, 2024]

        */
    }

    private static async Task AskQuestionsFilteringByTypeTag()
    {
        // Filter question by "type" tag, there are news but no articles
        var question = "What is Orion?";
        Console.WriteLine($"Question: {question}");

        var answer = await s_memory.AskAsync(question, filter: MemoryFilters.ByTag("type", "article"));
        Console.WriteLine($"\nArticles (none expected): {answer.Result}");

        answer = await s_memory.AskAsync(question, filter: MemoryFilters.ByTag("type", "news"));
        Console.WriteLine($"\nNews: {answer.Result}");

        Console.WriteLine("\n====================================\n");

        /* OUTPUT

        Question: What is Orion?

        Articles (none expected): INFO NOT FOUND
            warn: Microsoft.KernelMemory.Search.SearchClient[0]
            No memories available

        News: Orion is NASA's spacecraft designed for deep space exploration, including missions to the Moon and potentially Mars in the future. It is part
        of NASA's Artemis program, which aims to return humans to the Moon and establish a sustainable presence there as a stepping stone for further exploration.
        The Orion spacecraft is built to carry astronauts beyond low Earth orbit, and it is equipped with life support, emergency abort capabilities, and systems
        necessary for crew safety during long-duration missions. The Artemis II mission mentioned in the document will be the first crewed mission of the Artemis
        program, utilizing the Orion spacecraft to send four astronauts around the Moon to test its systems in preparation for future lunar missions.

        */
    }

    private static async Task AskQuestionsAboutExcelData()
    {
        var question = "Which countries don't have a long name set (explain rationale)?";
        Console.WriteLine($"Question: {question}");

        var answer = await s_memory.AskAsync(question, filter: MemoryFilters.ByDocument("xls01"));
        Console.WriteLine($"\nAnswer: {answer.Result}");

        Console.WriteLine("\n====================================\n");

        /* OUTPUT

        Question: Which countries don't have a long name set (explain rationale)?

        Answer: Based on the information provided in the worksheet from the file "file8-data.xlsx," the countries that don't have a long name set are "Italia" and
        "Japan." This is evident from the data in the "Long Name" column, where the corresponding entries for these countries are empty, indicating that the long
        name for these countries has not been provided or set in the worksheet. In contrast, the "U.S.A." has a long name set, which is "United States of America."

        */
    }

    private static async Task AskQuestionsAboutJsonFile()
    {
        var question = "What authentication mechanisms can I use with Azure Embeddings?";
        Console.WriteLine($"Question: {question}");

        var answer = await s_memory.AskAsync(question, filter: MemoryFilters.ByDocument("json01"));
        Console.WriteLine($"\nAnswer: {answer.Result}");

        Console.WriteLine("\n====================================\n");

        /* OUTPUT

        Question: What authentication mechanisms can I use with Azure Embeddings?

        Answer: For Azure Embeddings, you can use either an "APIKey" or "AzureIdentity" as authentication mechanisms. The "AzureIdentity" option utilizes an automatic
        Azure Active Directory (AAD) authentication mechanism. To test this locally, you can set the environment variables AZURE_TENANT_ID, AZURE_CLIENT_ID, and
        AZURE_CLIENT_SECRET. If you choose to use an "APIKey", you would need to provide the actual API key in the configuration.

        */
    }

    // Download file and print details
    private static async Task DownloadFile()
    {
        const string filename = "file1-Wikipedia-Carbon.txt";

        Console.WriteLine("Downloading file");
        StreamableFileContent result = await s_memory.ExportFileAsync(documentId: "doc001", fileName: filename);
        var stream = new MemoryStream();
        await (await result.GetStreamAsync()).CopyToAsync(stream);
        var bytes = stream.ToArray();

        Console.WriteLine();
        Console.WriteLine("Original File name : " + filename);
        Console.WriteLine("Original File size : " + new FileInfo(filename).Length);
        Console.WriteLine("Original Bytes count: " + (await File.ReadAllBytesAsync(filename)).Length);
        Console.WriteLine();
        Console.WriteLine("Downloaded File name : " + result.FileName);
        Console.WriteLine("Downloaded File type : " + result.FileType);
        Console.WriteLine("Downloaded File size : " + result.FileSize);
        Console.WriteLine("Downloaded Bytes count: " + bytes.Length);
    }

    // =======================
    // === PURGE =============
    // =======================

    private static async Task DeleteMemories()
    {
        foreach (var docId in s_toDelete)
        {
            Console.WriteLine($"Deleting memories derived from {docId}");
            await s_memory.DeleteDocumentAsync(docId);
        }
    }
}
