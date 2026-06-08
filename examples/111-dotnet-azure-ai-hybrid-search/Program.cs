// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;

public static class Program
{
    private const string IndexName = "acronyms";

    public static async Task Main()
    {
        var azureOpenAITextConfig = new AzureOpenAIConfig();
        var azureOpenAIEmbeddingConfig = new AzureOpenAIConfig();
        var azureAISearchConfigWithHybridSearch = new AzureAISearchConfig();
        var azureAISearchConfigWithoutHybridSearch = new AzureAISearchConfig();

        new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.development.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build()
            .BindSection("KernelMemory:Services:AzureOpenAIText", azureOpenAITextConfig)
            .BindSection("KernelMemory:Services:AzureOpenAIEmbedding", azureOpenAIEmbeddingConfig)
            .BindSection("KernelMemory:Services:AzureAISearch", azureAISearchConfigWithHybridSearch)
            .BindSection("KernelMemory:Services:AzureAISearch", azureAISearchConfigWithoutHybridSearch);

        azureAISearchConfigWithHybridSearch.UseHybridSearch = true;
        azureAISearchConfigWithoutHybridSearch.UseHybridSearch = false;

        var memoryNoHybridSearch = new KernelMemoryBuilder()
            .WithAzureOpenAITextGeneration(azureOpenAITextConfig)
            .WithAzureOpenAITextEmbeddingGeneration(azureOpenAIEmbeddingConfig)
            .WithAzureAISearchMemoryDb(azureAISearchConfigWithoutHybridSearch)
            .WithSearchClientConfig(new SearchClientConfig { MaxMatchesCount = 2, Temperature = 0, TopP = 0 })
            .Build<MemoryServerless>();

        var memoryWithHybridSearch = new KernelMemoryBuilder()
            .WithAzureOpenAITextGeneration(azureOpenAITextConfig)
            .WithAzureOpenAITextEmbeddingGeneration(azureOpenAIEmbeddingConfig)
            .WithAzureAISearchMemoryDb(azureAISearchConfigWithHybridSearch)
            .WithSearchClientConfig(new SearchClientConfig { MaxMatchesCount = 2, Temperature = 0, TopP = 0 })
            .Build<MemoryServerless>();

        await CreateIndexAndImportData(memoryWithHybridSearch);

        const string Question = "abc";

        Console.WriteLine("Answer without hybrid search:");
        await AskQuestion(memoryNoHybridSearch, Question);
        // Output: INFO NOT FOUND

        Console.WriteLine("Answer using hybrid search:");
        await AskQuestion(memoryWithHybridSearch, Question);
        // Output: 'Aliens Brewing Coffee'
    }

    private static async Task AskQuestion(IKernelMemory memory, string question)
    {
        var answer = await memory.AskAsync(question, index: IndexName);
        Console.WriteLine(answer.Result);
    }

    private static async Task CreateIndexAndImportData(IKernelMemory memory)
    {
        await memory.DeleteIndexAsync(IndexName);

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
            await memory.ImportTextAsync(acronym, index: IndexName);
        }
    }
}
