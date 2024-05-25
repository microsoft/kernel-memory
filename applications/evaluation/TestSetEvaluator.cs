// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.KernelMemory.Evaluators.AnswerCorrectness;
using Microsoft.KernelMemory.Evaluators.AnswerSimilarity;
using Microsoft.KernelMemory.Evaluators.ContextRecall;
using Microsoft.KernelMemory.Evaluators.ContextRelevancy;
using Microsoft.KernelMemory.Evaluators.Faithfulness;
using Microsoft.KernelMemory.Evaluators.Relevance;
using Microsoft.SemanticKernel;

namespace Microsoft.KernelMemory.Evaluation;

public sealed class TestSetEvaluator
{
    private readonly IKernelMemory _kernelMemory;
    private readonly string _indexName;

    private readonly Kernel _evaluatorKernel;

    private FaithfulnessEvaluator Faithfulness => new(this._evaluatorKernel);

    private RelevanceEvaluator Relevance => new(this._evaluatorKernel);

    private AnswerSimilarityEvaluator AnswerSimilarity => new(this._evaluatorKernel);

    private ContextRelevancyEvaluator ContextRelevancy => new(this._evaluatorKernel);

    private AnswerCorrectnessEvaluator AnswerCorrectness => new(this._evaluatorKernel);

    private ContextRecallEvaluator ContextRecall => new(this._evaluatorKernel);

    public TestSetEvaluator(IKernelBuilder evaluatorKernel, IKernelMemory kernelMemory, string indexName)
    {
        this._evaluatorKernel = evaluatorKernel.Build();

        this._kernelMemory = kernelMemory;
        this._indexName = indexName;
    }

    public async IAsyncEnumerable<QuestionEvaluation> EvaluateTestSetAsync(IEnumerable<TestSet.TestSetItem> questions)
    {
        foreach (var test in questions)
        {
            var answer = await this._kernelMemory.AskAsync(test.Question, this._indexName).ConfigureAwait(false);

            if (answer.NoResult)
            {
                yield return new QuestionEvaluation
                {
                    TestSet = test,
                    MemoryAnswer = answer,
                    Metrics = new()
                };

                continue;
            }

            var metadata = new Dictionary<string, object?>
            {
                { "Question", test.Question },
                { "IndexName", this._indexName }
            };

            yield return new QuestionEvaluation
            {
                TestSet = test,
                MemoryAnswer = answer,
                Metrics = new()
                {
                    AnswerRelevancy = await this.Relevance.Evaluate(answer, metadata).ConfigureAwait(false),
                    AnswerSemanticSimilarity = await this.AnswerSimilarity.Evaluate(test, answer, metadata).ConfigureAwait(false),
                    AnswerCorrectness = await this.AnswerCorrectness.Evaluate(test, answer, metadata).ConfigureAwait(false),
                    Faithfulness = await this.Faithfulness.Evaluate(answer, metadata).ConfigureAwait(false),
                    ContextPrecision = await this.ContextRelevancy.Evaluate(answer, metadata).ConfigureAwait(false),
                    ContextRecall = await this.ContextRecall.Evaluate(test, answer, metadata).ConfigureAwait(false)
                }
            };
        }
    }
}

public class QuestionEvaluation
{
    public TestSet.TestSetItem TestSet { get; set; } = null!;

    public MemoryAnswer MemoryAnswer { get; set; } = null!;

    public EvaluationMetrics Metrics { get; set; } = new EvaluationMetrics();

    public Dictionary<string, object?> Metadata { get; set; } = new();
}
