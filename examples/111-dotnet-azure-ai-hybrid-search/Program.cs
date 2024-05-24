// Copyright (c) Microsoft. All rights reserved.

// ReSharper disable InconsistentNaming

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI.OpenAI;

public static class Program
{
    private const string indexName = "acronyms";

    public static async Task Main()
    {
        var azureOpenAITextConfig = new AzureOpenAIConfig();
        var azureOpenAIEmbeddingConfig = new AzureOpenAIConfig();
        var azureAISearchConfigWithHybridSearch = new AzureAISearchConfig();
        var azureAISearchConfigWithoutHybridSearch = new AzureAISearchConfig();

        new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build()
            .BindSection("KernelMemory:Services:AzureOpenAIText", azureOpenAITextConfig)
            .BindSection("KernelMemory:Services:AzureOpenAIEmbedding", azureOpenAIEmbeddingConfig)
            .BindSection("KernelMemory:Services:AzureAISearch", azureAISearchConfigWithHybridSearch)
            .BindSection("KernelMemory:Services:AzureAISearch", azureAISearchConfigWithoutHybridSearch);

        azureAISearchConfigWithHybridSearch.UseHybridSearch = true;
        azureAISearchConfigWithoutHybridSearch.UseHybridSearch = false;

        var memoryNoHybridSearch = new KernelMemoryBuilder()
            .WithAzureOpenAITextGeneration(azureOpenAITextConfig, new DefaultGPTTokenizer())
            .WithAzureOpenAITextEmbeddingGeneration(azureOpenAIEmbeddingConfig, new DefaultGPTTokenizer())
            .WithAzureAISearchMemoryDb(azureAISearchConfigWithoutHybridSearch)
            .WithSearchClientConfig(new SearchClientConfig { MaxMatchesCount = 2, Temperature = 0, TopP = 0 })
            .Build<MemoryServerless>();

        var memoryWithHybridSearch = new KernelMemoryBuilder()
            .WithAzureOpenAITextGeneration(azureOpenAITextConfig, new DefaultGPTTokenizer())
            .WithAzureOpenAITextEmbeddingGeneration(azureOpenAIEmbeddingConfig, new DefaultGPTTokenizer())
            .WithAzureAISearchMemoryDb(azureAISearchConfigWithHybridSearch)
            .WithSearchClientConfig(new SearchClientConfig { MaxMatchesCount = 2, Temperature = 0, TopP = 0 })
            .Build<MemoryServerless>();

        await CreateIndexAndImportData(memoryWithHybridSearch);

        const string question = "abc";

        Console.WriteLine("Answer without hybrid search:");
        await AskQuestion(memoryNoHybridSearch, question);
        // Output: INFO NOT FOUND

        Console.WriteLine("Answer using hybrid search:");
        await AskQuestion(memoryWithHybridSearch, question);
        // Output: 'Aliens Brewing Coffee'
    }

    private static async Task AskQuestion(IKernelMemory memory, string question)
    {
        var answer = await memory.AskAsync(question, index: indexName);
        Console.WriteLine(answer.Result);
    }

    private static async Task CreateIndexAndImportData(IKernelMemory memory)
    {
        await memory.DeleteIndexAsync(indexName);

        var data = """
                   aaa bbb ccc 000000000
                   C B A   .......
                   ai bee cee  Something else
                   XY.  abc means  'Aliens Brewing Coffee'
                   abeec abecedario
                   A B C D  first 4 letters
                   """;

        var rows = data.Split("\n");
        foreach (var acronym in rows)
        {
            await memory.ImportTextAsync(acronym, index: indexName);
        }
    }
}
