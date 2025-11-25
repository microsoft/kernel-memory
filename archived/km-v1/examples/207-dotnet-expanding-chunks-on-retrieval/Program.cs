// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Configuration;
using Microsoft.KernelMemory.DocumentStorage.DevTools;
using Microsoft.KernelMemory.FileSystem.DevTools;
using Microsoft.KernelMemory.MemoryStorage.DevTools;

/// <summary>
/// This example shows how to retrieve N memory records before and after a relevant memory record.
///
/// Suppose uploading a book, and during the import KM splits the text in 10,000 partitions of 100 tokens, generating 10,000 memory records.
/// When searching memory by similarity, the system returns a list of relevant memories, containing snippets of text ~100 tokens long.
///
/// Before sending text snippets to a LLM along with a question (RAG), you might want to include extra information, e.g. text PRECEDING and FOLLOWING each text snippet, e.g. 100 tokens extra on both sides:
///
///     ----------
///     partition N - 1, memory record
///     text snippet
///     100 tokens
///     ----------
///     partition N, RELEVANT memory record
///     text snippet
///     100 tokens
///     ----------
///     partition N + 1, memory record
///     text snippet
///     100 tokens
///     ---------
///
/// The code below shows how to fetch records before and after each RELEVANT memory record, leveraging the Partition Number property.
///
/// Note: when importing documents, you can set `OverlappingTokens` so that each partition contains a part of the previous and the next partitions.
///       This is another approach to always include a little more context, however this approach is limited by the max number of tokens an
///       embedding generator can work with, and in a way affects the semantics of each text snippet.
///       Also, when using the example below, you should consider setting OverlappingTokens to zero, to avoid text repetitions.
/// </summary>
public static class Program
{
    public static async Task Main()
    {
        // Partition input text in chunks of 100 tokens
        const int Chunksize = 100;

        // Search settings
        const string Query = "astrobiology";
        const float MinRelevance = 0.7f;
        const int Limit = 2;

        // Load OpenAI settings and API key
        var openAIConfig = new OpenAIConfig();
        new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.development.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build()
            .BindSection("KernelMemory:Services:OpenAI", openAIConfig);

        // Customize memory records size (in tokens)
        var textPartitioningOptions = new TextPartitioningOptions
        {
            MaxTokensPerParagraph = Chunksize,
            OverlappingTokens = 0,
        };

        // Prepare memory instance, store memories on disk so import runs only once
        var memory = new KernelMemoryBuilder()
            .WithOpenAI(openAIConfig)
            .WithCustomTextPartitioningOptions(textPartitioningOptions)
            .WithSimpleFileStorage(new SimpleFileStorageConfig { StorageType = FileSystemTypes.Disk })
            .WithSimpleVectorDb(new SimpleVectorDbConfig { StorageType = FileSystemTypes.Disk })
            .Build();

        // Load text into memory
        Console.WriteLine("Importing memories...");
        await memory.ImportDocumentAsync(filePath: "story.docx", documentId: "example207");

        // Search
        Console.WriteLine("Searching memories...");
        SearchResult relevant = await memory.SearchAsync(query: Query, minRelevance: MinRelevance, limit: Limit);
        Console.WriteLine($"Relevant documents: {relevant.Results.Count}");

        foreach (Citation result in relevant.Results)
        {
            // Store the document IDs so we can load all their records later
            Console.WriteLine($"Document ID: {result.DocumentId}");
            Console.WriteLine($"Relevant partitions: {result.Partitions.Count}");
            foreach (Citation.Partition partition in result.Partitions)
            {
                Console.WriteLine($" * Partition {partition.PartitionNumber}, relevance: {partition.Relevance}");
            }

            Console.WriteLine("--------------------------");

            // For each relevant partition fetch the partition before and one after
            foreach (Citation.Partition partition in result.Partitions)
            {
                // Collect partitions in a sorted collection
                var partitions = new SortedDictionary<int, Citation.Partition> { [partition.PartitionNumber] = partition };

                // Filters to fetch adjacent partitions
                var filters = new List<MemoryFilter>
                {
                    MemoryFilters.ByDocument(result.DocumentId).ByTag(Constants.ReservedFilePartitionNumberTag, $"{partition.PartitionNumber - 1}"),
                    MemoryFilters.ByDocument(result.DocumentId).ByTag(Constants.ReservedFilePartitionNumberTag, $"{partition.PartitionNumber + 1}")
                };

                // Fetch adjacent partitions and add them to the sorted collection
                SearchResult adjacentList = await memory.SearchAsync("", filters: filters, limit: 2);
                foreach (Citation.Partition adjacent in adjacentList.Results.First().Partitions)
                {
                    partitions[adjacent.PartitionNumber] = adjacent;
                }

                // Print partitions in order
                foreach (var p in partitions)
                {
                    Console.WriteLine($"# Partition {p.Value.PartitionNumber}");
                    Console.WriteLine(p.Value.Text);
                    Console.WriteLine();
                }

                Console.WriteLine("--------------------------");
            }

            Console.WriteLine();
        }
    }
}

/* Result:

Importing memories...
Searching memories...
Relevant documents: 1
Document ID: example207
Relevant partitions: 2
* Partition 27, relevance: 0.8557962
* Partition 13, relevance: 0.85513425
--------------------------
# Partition 26
Dr. Mei Lin, a renowned ...

# Partition 27
As scientific interest in ...

# Partition 28
Meanwhile, back on Earth, the ...
--------------------------
# Partition 12
Appearing as a glowing, translucent ...

# Partition 13
Gerald Marshall, the Chief ...

# Partition 14
While further studies are ...
--------------------------
*/
