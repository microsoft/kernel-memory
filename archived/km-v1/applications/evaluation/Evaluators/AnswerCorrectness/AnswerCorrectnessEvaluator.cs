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
namespace Microsoft.KernelMemory.Evaluators.AnswerCorrectness;

internal sealed class AnswerCorrectnessEvaluator : EvaluationEngine
{
    private readonly Kernel _kernel;

    private KernelFunction ExtractStatements => this._kernel.CreateFunctionFromPrompt(this.GetSKPrompt("Extraction", "Statements"), new OpenAIPromptExecutionSettings
    {
        Temperature = 1e-8f,
        Seed = 0,
        ResponseFormat = "json_object"
    }, functionName: nameof(this.ExtractStatements));

    private KernelFunction EvaluateCorrectness => this._kernel.CreateFunctionFromPrompt(this.GetSKPrompt("Evaluation", "Correctness"), new OpenAIPromptExecutionSettings
    {
        Temperature = 1e-8f,
        Seed = 0,
        ResponseFormat = "json_object"
    }, functionName: nameof(this.EvaluateCorrectness));

    public AnswerCorrectnessEvaluator(Kernel kernel)
    {
        this._kernel = kernel.Clone();
    }

    internal async Task<float> Evaluate(TestSetItem testSet, MemoryAnswer answer, Dictionary<string, object?> metadata)
    {
        var statements = await this.Try(3, async (remainingTry) =>
        {
            var extraction = await this.ExtractStatements.InvokeAsync(this._kernel, new KernelArguments
            {
                { "question", answer.Question },
                { "answer", answer.Result }
            }).ConfigureAwait(false);

            return JsonSerializer.Deserialize<StatementExtraction>(extraction.GetValue<string>()!);
        }).ConfigureAwait(false);

        if (statements is null)
        {
            return 0;
        }

        var evaluation = await this.Try(3, async (remainingTry) =>
        {
            var extraction = await this.EvaluateCorrectness.InvokeAsync(this._kernel, new KernelArguments
            {
                { "question", answer.Question },
                { "answer", JsonSerializer.Serialize(statements) },
                { "ground_truth", JsonSerializer.Serialize(testSet.Context) }
            }).ConfigureAwait(false);

            return JsonSerializer.Deserialize<CorrectnessEvaluation>(extraction.GetValue<string>()!);
        }).ConfigureAwait(false);

        if (evaluation is null)
        {
            return 0;
        }

        metadata.Add($"{nameof(AnswerCorrectnessEvaluator)}-Evaluation", evaluation);

        return (float)evaluation.TP.Count() /
               (float)(evaluation.TP.Count() + .5 * (evaluation.FP.Count() + evaluation.FN.Count()));
    }
}
