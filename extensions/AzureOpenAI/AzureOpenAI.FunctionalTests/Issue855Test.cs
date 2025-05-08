// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Configuration;
using Microsoft.KernelMemory.AI.AzureOpenAI;
using Microsoft.KM.TestHelpers;

namespace Microsoft.AzureOpenAI.FunctionalTests;

/// <summary>
/// References:
/// - https://github.com/Azure/azure-sdk-for-net/issues/46109
/// - https://github.com/microsoft/semantic-kernel/issues/8929
/// - https://github.com/microsoft/kernel-memory/issues/855
/// </summary>
public class Issue855Test : BaseFunctionalTestCase
{
    private readonly AzureOpenAITextEmbeddingGenerator _target;

    public Issue855Test(IConfiguration cfg, ITestOutputHelper output) : base(cfg, output)
    {
        this._target = new AzureOpenAITextEmbeddingGenerator(this.AzureOpenAIEmbeddingConfiguration);
    }

    // [Fact] // Enable manually on a need basis
    [Fact(Skip = "Enable and run manually")]
    [Trait("Category", "Manual")]
    [Trait("Category", "BugFix")]
    public async Task ItDoesntFailWhenThrottling()
    {
        for (int i = 0; i < 50; i++)
        {
            Console.WriteLine($"## {i}");
            await this._target.GenerateEmbeddingBatchAsync(
                [RndStr(), RndStr(), RndStr(), RndStr(), RndStr(), RndStr(), RndStr(), RndStr(), RndStr(), RndStr()]);
        }
    }

#pragma warning disable CA5394
    private static string RndStr()
    {
        var random = new Random();
        return new(Enumerable.Repeat(" ABCDEFGHIJKLMNOPQRSTUVWXYZ abcdefghijklmnopqrstuvwxyz 0123456789 ", 8000)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}

#pragma warning disable IDE0055
/* When the test fails: after pausing and trying to restart, an exception occurs.

Microsoft.SemanticKernel.HttpOperationException: Service request failed.

Microsoft.SemanticKernel.HttpOperationException
Service request failed.
Status: 401 (Unauthorized) <===== ******* Caused by https://github.com/Azure/azure-sdk-for-net/issues/46109

  at Microsoft.SemanticKernel.Connectors.OpenAI.ClientCore.RunRequestAsync[T](Func`1 request)
  at Microsoft.SemanticKernel.Connectors.OpenAI.ClientCore.GetEmbeddingsAsync(String targetModel, IList`1 data, Kernel kernel, Nullable`1 dimensions, CancellationToken cancellationToken)
  at Microsoft.KernelMemory.AI.AzureOpenAI.AzureOpenAITextEmbeddingGenerator.GenerateEmbeddingBatchAsync(IEnumerable`1 textList, CancellationToken cancellationToken) in extensions/AzureOpenAI/AzureOpenAI/AzureOpenAITextEmbeddingGenerator.cs:line 132
  at Microsoft.AzureOpenAI.FunctionalTests.Issue855Test.ItDoesntFailWith401() in extensions/AzureOpenAI/AzureOpenAI.FunctionalTests/Bug46109Test.cs:line 43
  at Xunit.DependencyInjection.DependencyInjectionTestInvoker.AsyncStack(Task task, Activity activity) in S:\GitHub\Xunit.DependencyInjection\src\Xunit.DependencyInjection\DependencyInjectionTestInvoker.cs:line 174

System.ClientModel.ClientResultException
Service request failed.
Status: 401 (Unauthorized)

  at Azure.AI.OpenAI.ClientPipelineExtensions.ProcessMessageAsync(ClientPipeline pipeline, PipelineMessage message, RequestOptions options)
  at Azure.AI.OpenAI.Embeddings.AzureEmbeddingClient.GenerateEmbeddingsAsync(BinaryContent content, RequestOptions options)
  at OpenAI.Embeddings.EmbeddingClient.GenerateEmbeddingsAsync(IEnumerable`1 inputs, EmbeddingGenerationOptions options, CancellationToken cancellationToken)
  at Microsoft.SemanticKernel.Connectors.OpenAI.ClientCore.RunRequestAsync[T](Func`1 request)



warn: Microsoft.KernelMemory.AI.AzureOpenAI.AzureOpenAITextEmbeddingGenerator[0]
     Tokenizer not specified, will use GPT4oTokenizer. The token count might be incorrect, causing unexpected errors

## 0
## 1
## 2
## 3
...
...
warn: Microsoft.KernelMemory.AI.AzureOpenAI.Internals.ClientSequentialRetryPolicy[0]
     Header Retry-After found, value 21

warn: Microsoft.KernelMemory.AI.AzureOpenAI.Internals.ClientSequentialRetryPolicy[0]
     Delay extracted from HTTP response: 21000 msecs
*/
