// Copyright (c) Microsoft. All rights reserved.

using FunctionalTests.TestHelpers;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;
using Xunit.Abstractions;

namespace FunctionalTests.Service;

public class PluginTest : BaseTestCase
{
    public PluginTest(IConfiguration cfg, ITestOutputHelper output) : base(cfg, output)
    {
    }

    [Fact]
    public async Task ItSupportsQuestionsOnUploadedFiles()
    {
        // Arrange
        var config = this.Configuration.GetSection("Services").GetSection("AzureOpenAIText");
        var kernel = Kernel.CreateBuilder().AddAzureOpenAIChatCompletion(
            deploymentName: config.GetValue<string>("Deployment") ?? "",
            modelId: config.GetValue<string>("Deployment") ?? "",
            endpoint: config.GetValue<string>("Endpoint") ?? "",
            apiKey: config.GetValue<string>("APIKey") ?? "").Build();

        var skPrompt = """
                       Question to Kernel Memory: {{$input}}

                       Kernel Memory Answer: {{memory.ask $input}}

                       If the answer is empty say 'I don't know' otherwise reply with a preview of the answer, truncated to 15 words.
                       """;

        KernelFunction myFunction = kernel.CreateFunctionFromPrompt(skPrompt);

        IKernelMemory memory = this.GetMemoryWebClient();
        var memoryPlugin = kernel.ImportPluginFromObject(new MemoryPlugin(memory, waitForIngestionToComplete: true), "memory");
        var context = new KernelArguments
        {
            [MemoryPlugin.FilePathParam] = "file1-NASA-news.pdf",
            [MemoryPlugin.DocumentIdParam] = "NASA001"
        };

        // Act
        await memoryPlugin["SaveFile"].InvokeAsync(kernel, context);
        var answer = await myFunction.InvokeAsync(kernel, "any news about Orion?");

        // Assert
        Console.WriteLine(answer);
        Assert.DoesNotContain("I don't know", answer.GetValue<string>());
    }
}
