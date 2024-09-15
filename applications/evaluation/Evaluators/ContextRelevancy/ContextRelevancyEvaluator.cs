// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.KernelMemory.Evaluation;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable CheckNamespace
namespace Microsoft.KernelMemory.Evaluators.ContextRelevancy;

internal sealed class ContextRelevancyEvaluator : EvaluationEngine
{
    private readonly Kernel _kernel;

    private KernelFunction EvaluateContext => this._kernel.CreateFunctionFromPrompt(this.GetSKPrompt("Evaluation", "ContextPrecision"), new OpenAIPromptExecutionSettings
    {
        Temperature = 1e-8f,
        Seed = 0,
        ResponseFormat = "json_object"
    });

    public ContextRelevancyEvaluator(Kernel kernel)
    {
        this._kernel = kernel.Clone();
    }

    internal async Task<float> Evaluate(MemoryAnswer answer, Dictionary<string, object?> metadata)
    {
        var contextRelevancy = new List<ContextRelevancy>();

        foreach (var item in answer.RelevantSources.SelectMany(c => c.Partitions))
        {
            contextRelevancy.Add(await this.EvaluateContextRelevancy(item, answer).ConfigureAwait(false));
        }

        metadata.Add($"{nameof(ContextRelevancyEvaluator)}-Evaluation", contextRelevancy);

        return contextRelevancy.Count(c => c.Verdict > 0) / (float)contextRelevancy.Count;
    }

    internal async Task<ContextRelevancy> EvaluateContextRelevancy(Citation.Partition partition, MemoryAnswer answer)
    {
        var relevancy = await this.Try(3, async (tryCount) =>
        {
            var verification = await this.EvaluateContext.InvokeAsync(this._kernel, new KernelArguments
            {
                { "question", answer.Question },
                { "answer", answer.Result },
                { "context", partition.Text }
            }).ConfigureAwait(false);

            return JsonSerializer.Deserialize<ContextRelevancy>(verification.GetValue<string>()!);
        }).ConfigureAwait(false);

        relevancy!.PartitionText = partition.Text;

        return relevancy;
    }
}
