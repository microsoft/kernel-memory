// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using System.Numerics.Tensors;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.Evaluation;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable CheckNamespace
namespace Microsoft.KernelMemory.Evaluators.Relevance;

internal sealed class RelevanceEvaluator : EvaluationEngine
{
    private readonly Kernel _kernel;

    private readonly ITextEmbeddingGenerationService _textEmbeddingGenerationService;

    private KernelFunction ExtractQuestion => this._kernel.CreateFunctionFromPrompt(this.GetSKPrompt("Extraction", "Question"), new OpenAIPromptExecutionSettings
    {
        Temperature = 1e-8f,
        Seed = 0,
        ResponseFormat = "json_object"
    }, functionName: nameof(this.ExtractQuestion));

    public RelevanceEvaluator(Kernel kernel)
    {
        this._kernel = kernel.Clone();

        this._textEmbeddingGenerationService = this._kernel.Services.GetRequiredService<ITextEmbeddingGenerationService>();
    }

    internal async Task<float> Evaluate(MemoryAnswer answer, Dictionary<string, object?> metadata, int strictness = 3)
    {
        var questionEmbeddings = await this._textEmbeddingGenerationService
            .GenerateEmbeddingsAsync([answer.Question], this._kernel)
            .ConfigureAwait(false);

        var generatedQuestions = await this.GetEvaluations(answer, strictness)
            .ToArrayAsync()
            .ConfigureAwait(false);

        var generatedQuestionsEmbeddings = await this._textEmbeddingGenerationService
            .GenerateEmbeddingsAsync(generatedQuestions.Select(c => c.Question).ToArray(), this._kernel)
            .ConfigureAwait(false);

        var evaluations = generatedQuestionsEmbeddings
            .Select(c => TensorPrimitives.CosineSimilarity(questionEmbeddings.Single().Span, c.Span)
                         *
                         generatedQuestions[generatedQuestionsEmbeddings.IndexOf(c)].Committal);

        metadata.Add($"{nameof(RelevanceEvaluator)}-Evaluation", evaluations);

        return evaluations.Average();
    }

    private async IAsyncEnumerable<RelevanceEvaluation> GetEvaluations(MemoryAnswer answer, int strictness)
    {
        foreach (var item in Enumerable.Range(0, strictness))
        {
            var statements = await this.Try(3, async (remainingTry) =>
            {
                var extraction = await this.ExtractQuestion.InvokeAsync(this._kernel, new KernelArguments
                {
                    { "context", string.Join('\n', answer.RelevantSources.SelectMany(c => c.Partitions.Select(p => p.Text))) },
                    { "answer", answer.Result }
                }).ConfigureAwait(false);

                return JsonSerializer.Deserialize<RelevanceEvaluation>(extraction.GetValue<string>()!);
            }).ConfigureAwait(false);

            yield return statements!;
        }
    }
}
