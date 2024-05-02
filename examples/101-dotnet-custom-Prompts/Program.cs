// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Prompts;

public static class Program
{
    // ReSharper disable InconsistentNaming
    public static async Task Main()
    {
        var azureOpenAITextConfig = new AzureOpenAIConfig();
        var azureOpenAIEmbeddingConfig = new AzureOpenAIConfig();
        var openAIConfig = new OpenAIConfig();
        var searchClientConfig = new SearchClientConfig();

        new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build()
            .BindSection("KernelMemory:Services:OpenAI", openAIConfig)
            .BindSection("KernelMemory:Services:AzureOpenAIText", azureOpenAITextConfig)
            .BindSection("KernelMemory:Services:AzureOpenAIEmbedding", azureOpenAIEmbeddingConfig)
            .BindSection("KernelMemory:Retrieval:SearchClient", searchClientConfig);

        var memory = new KernelMemoryBuilder()
            .WithCustomPromptProvider(new MyPromptProvider())
            // .WithOpenAIDefaults(Environment.GetEnvironmentVariable("OPENAI_API_KEY"))
            // .WithOpenAI(openAICfg)
            .WithAzureOpenAITextGeneration(azureOpenAITextConfig)
            .WithAzureOpenAITextEmbeddingGeneration(azureOpenAIEmbeddingConfig)
            .Build<MemoryServerless>();

        await memory.ImportTextAsync("NASA space probe Lucy flies by asteroid 152830 Dinkinesh, the first of eight asteroids planned to be visited by the spacecraft.");

        var statement = "Lucy flied by an asteroid";
        var verification = await memory.AskAsync(statement);
        Console.WriteLine($"{statement} => {verification.Result}");

        statement = "Lucy landed on an asteroid";
        verification = await memory.AskAsync(statement);
        Console.WriteLine($"{statement} => {verification.Result}");

        statement = "Lucy is powered by a nuclear engine";
        verification = await memory.AskAsync(statement);
        Console.WriteLine($"{statement} => {verification.Result}");

        /* OUTPUT *

        Lucy flied by an asteroid => TRUE
        Lucy landed on an asteroid => FALSE
        Lucy is powered by a nuclear engine => NEED MORE INFO

        */
    }
}

public class MyPromptProvider : IPromptProvider
{
    private const string VerificationPrompt = """
                                              Facts:
                                              {{$facts}}
                                              ======
                                              Given only the facts above, verify the fact below.
                                              You don't know where the knowledge comes from, just answer.
                                              If you have sufficient information to verify, reply only with 'TRUE', nothing else.
                                              If you have sufficient information to deny, reply only with 'FALSE', nothing else.
                                              If you don't have sufficient information, reply with 'NEED MORE INFO'.
                                              User: {{$input}}
                                              Verification: 
                                              """;

    private readonly EmbeddedPromptProvider _fallbackProvider = new();

    public string ReadPrompt(string promptName)
    {
        switch (promptName)
        {
            case Constants.PromptNamesAnswerWithFacts:
                return VerificationPrompt;

            default:
                // Fall back to the default
                return this._fallbackProvider.ReadPrompt(promptName);
        }
    }
}
