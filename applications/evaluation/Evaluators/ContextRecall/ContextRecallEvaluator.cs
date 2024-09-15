// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.KernelMemory.Evaluation;
using Microsoft.KernelMemory.Evaluation.TestSet;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable CheckNamespace
namespace Microsoft.KernelMemory.Evaluators.ContextRecall;

internal sealed class ContextRecallEvaluator : EvaluationEngine
{
    private readonly Kernel _kernel;

    private KernelFunction EvaluateContextRecall => this._kernel.CreateFunctionFromPrompt(this.GetSKPrompt("Evaluation", "ContextRecall"), new OpenAIPromptExecutionSettings
    {
        Temperature = 1e-8f,
        Seed = 0,
        ResponseFormat = "json_object"
    }, functionName: nameof(this.EvaluateContextRecall));

    public ContextRecallEvaluator(Kernel kernel)
    {
        this._kernel = kernel.Clone();
    }

    internal async Task<float> Evaluate(TestSetItem testSet, MemoryAnswer answer, Dictionary<string, object?> metadata)
    {
        var classification = await this.Try(3, async (remainingTry) =>
        {
            var extraction = await this.EvaluateContextRecall.InvokeAsync(this._kernel, new KernelArguments
            {
                { "question", testSet.Question },
                { "context", JsonSerializer.Serialize(answer.RelevantSources.SelectMany(c => c.Partitions.Select(x => x.Text))) },
                { "ground_truth", testSet.GroundTruth }
            }).ConfigureAwait(false);

            return JsonSerializer.Deserialize<GroundTruthClassifications>(extraction.GetValue<string>()!);
        }).ConfigureAwait(false);

        if (classification is null)
        {
            return 0;
        }

        metadata.Add($"{nameof(ContextRecallEvaluator)}-Evaluation", classification);

        return (float)classification.Evaluations.Count(c => c.Attributed > 0) / (float)classification.Evaluations.Count;
    }
}
