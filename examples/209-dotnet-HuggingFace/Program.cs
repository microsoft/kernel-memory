// Copyright (c) Microsoft. All rights reserved.

// ReSharper disable InconsistentNaming

using Microsoft.KernelMemory;

internal static class Program
{
    internal static async Task Main()
    {
        // Using HuggingFace text generation
        var huggingFaceConfig = new OpenAIConfig
        {
            Endpoint = "https://api-inference.huggingface.co/models/NousResearch/Nous-Hermes-2-Mixtral-8x7B-DPO/v1",
            TextModel = "NousResearch/Nous-Hermes-2-Mixtral-8x7B-DPO",
            TextModelMaxTokenTotal = 4096,
            TextGenerationType = OpenAIConfig.TextGenerationTypes.Chat,
            APIKey = Environment.GetEnvironmentVariable("HF_API_KEY")!
        };

        // Using OpenAI for embeddings
        var openAIEmbeddingConfig = new OpenAIConfig
        {
            EmbeddingModel = "text-embedding-ada-002",
            EmbeddingModelMaxTokenTotal = 8191,
            APIKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!
        };

        var memory = new KernelMemoryBuilder()
            // IMPORTANT: TopP = 0.01 is required by HuggingFace API
            .WithSearchClientConfig(new SearchClientConfig { TopP = 0.01, AnswerTokens = 100 })
            .WithOpenAITextGeneration(huggingFaceConfig) // Hugging Face
            .WithOpenAITextEmbeddingGeneration(openAIEmbeddingConfig) // OpenAI
            .Build();

        // Import some text - This will use OpenAI embeddings
        await memory.ImportTextAsync("Today is December 12th");

        // Generate an answer - This uses OpenAI for embeddings and finding relevant data, and Hugging Face to generate an answer
        var answer = await memory.AskAsync("How many days to Christmas?");
        Console.WriteLine(answer.Question);
        Console.WriteLine(answer.Result);
    }
}
