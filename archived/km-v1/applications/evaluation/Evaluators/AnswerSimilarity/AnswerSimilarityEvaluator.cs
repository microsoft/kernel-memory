// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using System.Numerics.Tensors;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.Evaluation;
using Microsoft.KernelMemory.Evaluation.TestSet;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable CheckNamespace
namespace Microsoft.KernelMemory.Evaluators.AnswerSimilarity;

internal sealed class AnswerSimilarityEvaluator : EvaluationEngine
{
    private readonly Kernel _kernel;

    private readonly ITextEmbeddingGenerationService _textEmbeddingGenerationService;

    public AnswerSimilarityEvaluator(Kernel kernel)
    {
        this._kernel = kernel.Clone();

        this._textEmbeddingGenerationService = this._kernel.Services.GetRequiredService<ITextEmbeddingGenerationService>();
    }

    internal async Task<float> Evaluate(TestSetItem testSet, MemoryAnswer answer, Dictionary<string, object?> metadata)
    {
        var answerEmbeddings = await this._textEmbeddingGenerationService
            .GenerateEmbeddingsAsync([testSet.GroundTruth, answer.Result], this._kernel)
            .ConfigureAwait(false);

        var evaluation = TensorPrimitives.CosineSimilarity(answerEmbeddings.First().Span, answerEmbeddings.Last().Span);

        metadata.Add($"{nameof(AnswerSimilarityEvaluator)}-Evaluation", evaluation);

        return evaluation;
    }
}
