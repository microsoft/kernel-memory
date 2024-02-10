// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI.OpenAI;
using Microsoft.KernelMemory.Configuration;
using Microsoft.KernelMemory.ContentStorage.DevTools;
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
    // ReSharper disable once InconsistentNaming
    public static async Task Main()
    {
        // Partition input text in chunks of 100 tokens
        const int PartitionSize = 100;

        // Some sample long content
        string story = await File.ReadAllTextAsync("story.txt");
        const string Query = "astrobiology";
        const float MinRelevance = 0.7f;
        const int Limit = 2;

        // Print the content size in tokens
        var tokenCount = DefaultGPTTokenizer.StaticCountTokens(story);
        Console.WriteLine($"Token count: {tokenCount}");

        // Load OpenAI settings and API key
        var openAIConfig = new OpenAIConfig();
        new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build()
            .BindSection("KernelMemory:Services:OpenAI", openAIConfig);

        // Customize memory records size (in tokens)
        var textPartitioningOptions = new TextPartitioningOptions
        {
            MaxTokensPerParagraph = PartitionSize,
            MaxTokensPerLine = PartitionSize,
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
        await memory.ImportTextAsync(story, documentId: "example207");

        // Search
        Console.WriteLine("Searching memories...");
        SearchResult relevant = await memory.SearchAsync(query: Query, minRelevance: MinRelevance, limit: Limit);
        Console.WriteLine($"Relevant documents: {relevant.Results.Count}");

#if KernelMemoryDev
        var relevantDocuments = new Dictionary<string, List<int>>();
        foreach (Citation result in relevant.Results)
        {
            // Store the document IDs so we can load all their records later
            relevantDocuments.Add(result.DocumentId, new List<int>());
            Console.WriteLine($"Document ID: {result.DocumentId}");
            Console.WriteLine($"Relevant partitions: {result.Partitions.Count}");
            foreach (Citation.Partition partition in result.Partitions)
            {
                Console.WriteLine("--------------------------");
                Console.WriteLine($"Partition number: {partition.PartitionNumber}");
                Console.WriteLine($"Relevance: {partition.Relevance}\n");
                Console.WriteLine(partition.Text);

                relevantDocuments[result.DocumentId].Add(partition.PartitionNumber);
            }

            Console.WriteLine();
        }

        // For each relevant document
        // Note: loops can be optimized for better perf, this code is only a demo
        const int HowManyToAdd = 1;
        Console.WriteLine("Fetching all document partitions...");
        foreach (KeyValuePair<string, List<int>> relevantPartitionNumbers in relevantDocuments)
        {
            var docId = relevantPartitionNumbers.Key;
            Console.WriteLine($"\nDocument ID: {docId}");

            // Load all partitions. Note: the list might be out of order.
            SearchResult all = await memory.SearchAsync("", filters: new[] { MemoryFilters.ByDocument(docId) }, limit: int.MaxValue);
            List<Citation.Partition> allPartitionsContent = all.Results.FirstOrDefault()?.Partitions ?? new();

            // Loop through the relevant partitions
            foreach (int relevantPartitionNumber in relevantPartitionNumbers.Value)
            {
                Console.WriteLine("--------------------------");

                // Use a data structure to order partitions by number
                var result = new SortedDictionary<int, string>();

                // Loop all partitions, include <HowManyToAdd> before and <HowManyToAdd> after the relevant ones
                foreach (Citation.Partition p in allPartitionsContent)
                {
                    if (Math.Abs(p.PartitionNumber - relevantPartitionNumber) <= HowManyToAdd)
                    {
                        result.Add(p.PartitionNumber, p.Text);
                    }
                }

                // Show partition and adjacent ones in order
                foreach (var p in result)
                {
                    Console.WriteLine($"Partition: {p.Key}");
                    Console.WriteLine(p.Value);
                }

                Console.WriteLine();
            }
        }
#endif
    }
}

/* Result:

Token count: 2510
Importing memories...
Searching memories...
Relevant documents: 1
Document ID: example207
Relevant partitions: 2
--------------------------
Partition number: 27
Relevance: 0.8557962

As scientific interest in [...] or ancient microbial life.
--------------------------
Partition number: 13
Relevance: 0.85513425

Gerald Marshall, the Chief [...] in astrobiological research."

Fetching all document partitions...

Document ID: example207
--------------------------
Partition: 26
Dr. Mei Lin, a renowned [...] of life in the universe."
Partition: 27
As scientific interest [...] ancient microbial life.
Partition: 28
Meanwhile, back on Earth, [...] meaning in the universe.

--------------------------
Partition: 12
Appearing as a glowing, [...] including its high CO2 levels.
Partition: 13
Gerald Marshall, the [...] in astrobiological research."
Partition: 14
While further studies [...] alien at the same time.

*/

