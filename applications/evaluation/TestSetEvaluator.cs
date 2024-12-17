// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.Evaluation.TestSet;
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
    private readonly Kernel _evaluatorKernel;

    private FaithfulnessEvaluator Faithfulness => new(this._evaluatorKernel);

    private RelevanceEvaluator Relevance => new(this._evaluatorKernel);

    private AnswerSimilarityEvaluator AnswerSimilarity => new(this._evaluatorKernel);

    private ContextRelevancyEvaluator ContextRelevancy => new(this._evaluatorKernel);

    private AnswerCorrectnessEvaluator AnswerCorrectness => new(this._evaluatorKernel);

    private ContextRecallEvaluator ContextRecall => new(this._evaluatorKernel);

    internal TestSetEvaluator([FromKeyedServices("evaluation")] Kernel evaluatorKernel, IKernelMemory kernelMemory)
    {
        this._evaluatorKernel = evaluatorKernel.Clone();
        this._kernelMemory = kernelMemory;
    }

    /// <summary>
    /// Evaluate a set of questions against the memory
    /// </summary>
    /// <param name="index">Optional index name</param>
    /// <param name="questions">The questions to evaluate</param>
    /// <param name="filters">Filters to match (using inclusive OR logic). If test contains filters too, the value is merged.</param>
    /// <returns></returns>
    public async IAsyncEnumerable<QuestionEvaluation> EvaluateTestSetAsync(
        string index,
        IEnumerable<TestSetItem> questions,
        IList<MemoryFilter>? filters = null!)
    {
        foreach (var test in questions)
        {
            var answer = await this._kernelMemory.AskAsync(test.Question, index, filters: this.MergeFilters(filters, test.Filters))
                .ConfigureAwait(false);

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
                { "IndexName", index },
                { "Filters", filters }
            };

            yield return new QuestionEvaluation
            {
                TestSet = test,
                MemoryAnswer = answer,
                Metadata = metadata,
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

    private ICollection<MemoryFilter>? MergeFilters(IList<MemoryFilter>? globalFilters, ICollection<MemoryFilter>? testFilters)
    {
        if (testFilters == null)
        {
            return globalFilters;
        }

        var filters = new Collection<MemoryFilter>();

        foreach (var filter in testFilters)
        {
            var merged = new MemoryFilter();
            filter.CopyTo(merged);

            if (globalFilters == null)
            {
                filters.Add(merged);
                continue;
            }

            foreach (var globalFilter in globalFilters)
            {
                filter.CopyTo(merged);
            }

            filters.Add(merged);
        }

        return filters;
    }
}

public class QuestionEvaluation
{
    public TestSetItem TestSet { get; set; } = null!;

    public MemoryAnswer MemoryAnswer { get; set; } = null!;

    public EvaluationMetrics Metrics { get; set; } = new();

    public Dictionary<string, object?> Metadata { get; set; } = [];
}
