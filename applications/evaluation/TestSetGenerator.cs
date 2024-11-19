// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.Evaluation.TestSet;
using Microsoft.KernelMemory.Evaluators.AnswerCorrectness;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Microsoft.KernelMemory.Evaluation;

public sealed partial class TestSetGenerator : EvaluationEngine
{
    private readonly IMemoryDb _memory;

    private readonly Kernel _evaluatorKernel;
    private readonly Kernel _translationKernel;

    private KernelFunction Translate => this._evaluatorKernel.CreateFunctionFromPrompt(this.GetSKPrompt("Transmutation", "Translate"), new OpenAIPromptExecutionSettings
    {
        Temperature = 1e-8f,
        Seed = 0
    });

    private KernelFunction QuestionAnswerGeneration => this._evaluatorKernel.CreateFunctionFromPrompt(this.GetSKPrompt("SyntheticData", "QuestionAnswer"), new OpenAIPromptExecutionSettings
    {
        Temperature = 1e-8f,
        Seed = 0,
        ResponseFormat = "json_object"
    });

    private KernelFunction KeyPhraseExtraction => this._evaluatorKernel.CreateFunctionFromPrompt(this.GetSKPrompt("Extraction", "Keyphrase"), new OpenAIPromptExecutionSettings
    {
        Temperature = 1e-8f,
        Seed = 0,
        ResponseFormat = "json_object"
    });

    private KernelFunction SeedQuestionGeneration => this._evaluatorKernel.CreateFunctionFromPrompt(this.GetSKPrompt("SyntheticData", "SeedQuestion"), new OpenAIPromptExecutionSettings
    {
        Temperature = 1e-8f,
        Seed = 0
    });

    private KernelFunction ReasoningQuestionGeneration => this._evaluatorKernel.CreateFunctionFromPrompt(this.GetSKPrompt("SyntheticData", "ReasoningQuestion"), new OpenAIPromptExecutionSettings
    {
        Temperature = 1e-8f,
        Seed = 0
    });

    private KernelFunction MultiContextQuestionGeneration => this._evaluatorKernel.CreateFunctionFromPrompt(this.GetSKPrompt("SyntheticData", "MultiContextQuestion"), new OpenAIPromptExecutionSettings
    {
        Temperature = 1e-8f,
        Seed = 0
    });

    private KernelFunction ConditioningQuestionGeneration => this._evaluatorKernel.CreateFunctionFromPrompt(this.GetSKPrompt("SyntheticData", "ConditionalQuestion"), new OpenAIPromptExecutionSettings
    {
        Temperature = 1e-8f,
        Seed = 0
    });

    internal TestSetGenerator(
        [FromKeyedServices("evaluation")] Kernel evaluationKernel,
        [FromKeyedServices("translation")] Kernel? translationKernel,
        IMemoryDb memoryDb)
    {
        this._evaluatorKernel = evaluationKernel.Clone();
        this._translationKernel = (translationKernel ?? evaluationKernel).Clone();
        this._memory = memoryDb;
    }

    public async IAsyncEnumerable<TestSetItem> GenerateTestSetsAsync(
        string index,
        int count = 10,
        int retryCount = 3,
        string language = null!,
        Distribution? distribution = null)
    {
        distribution ??= new Distribution();

        if (distribution.Value.Simple + distribution.Value.Reasoning + distribution.Value.MultiContext + distribution.Value.Conditioning != 1)
        {
            throw new ArgumentException("The sum of distribution values must be 1.");
        }

        var simpleCount = (int)(Math.Ceiling(count * distribution.Value.Simple));
        var reasoningCount = (int)(Math.Floor(count * distribution.Value.Reasoning));
        var multiContextCount = (int)(Math.Round(count * distribution.Value.MultiContext));
        var conditioningCount = (int)(Math.Round(count * distribution.Value.Conditioning));

        var documentIds = new List<string>();

        await foreach (var record in this._memory.GetListAsync(index, limit: int.MaxValue).ConfigureAwait(false))
        {
            if (documentIds.Contains(record.GetDocumentId()))
            {
                continue;
            }

            documentIds.Add(record.GetDocumentId());
        }

        foreach (var documentId in documentIds)
        {
            var partitionRecords = await this._memory.GetListAsync(index,
                    filters: [new MemoryFilter().ByDocument(documentId)],
                    limit: int.MaxValue)
                .ToArrayAsync()
                .ConfigureAwait(false);

            var nodes = this.SplitRecordsIntoNodes(partitionRecords, count);

            var questions =
                this.GetSimpleQuestionTestSetsAsync(nodes.Take(simpleCount), language: language, retryCount: retryCount)
                    .Concat(this.GetReasoningTestSetsAsync(nodes.Skip(simpleCount).Take(reasoningCount), language: language, retryCount: retryCount))
                    .Concat(this.GetMultiContextTestSetsAsync(nodes.Skip(simpleCount + reasoningCount).Take(multiContextCount), language: language, retryCount: retryCount))
                    .Concat(this.GetConditioningTestSetsAsync(nodes.Skip(simpleCount + reasoningCount + multiContextCount).Take(conditioningCount), language: language, retryCount: retryCount));

            await foreach (var item in questions.ConfigureAwait(false))
            {
                yield return item;
            }
        }
    }

    private async IAsyncEnumerable<TestSetItem> GetMultiContextTestSetsAsync(
        IEnumerable<MemoryRecord[]> nodes,
        string language = null!,
        int retryCount = 3)
    {
        foreach (var partition in nodes)
        {
            if (partition.Length < 2)
            {
                continue;
            }

            var seedQuestionContext = partition.First().GetPartitionText();
            var alternativeContext = partition.Last().GetPartitionText();

            var seedQuestion = await this.GetQuestionSeedAsync(seedQuestionContext, language, retryCount).ConfigureAwait(false);

            var question = await this.GetMultiContextQuestionAsync(seedQuestionContext, alternativeContext, seedQuestion, language, retryCount).ConfigureAwait(false);

            var groundTruth = await this.GetQuestionAnswerAsync(seedQuestionContext + " " + alternativeContext, question, language, retryCount).ConfigureAwait(false);

            var testSet = new TestSetItem
            {
                Question = seedQuestion,
                QuestionType = QuestionType.MultiContext,
                GroundTruth = groundTruth.Answer,
                GroundTruthVerdict = groundTruth.Verdict,
                Context = [seedQuestionContext, alternativeContext]
            };

            yield return testSet;
        }
    }

    private Task<string> GetMultiContextQuestionAsync(string context1, string context2, string seedQuestion, string language = null!, int retryCount = 3)
    {
        return this.Try(retryCount, async (remainingTry) =>
        {
            var question = await this.MultiContextQuestionGeneration.InvokeAsync(this._evaluatorKernel, new KernelArguments
            {
                { "question", seedQuestion },
                { "context1", context1 },
                { "context2", context2 }
            }).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(language))
            {
                question = await this.Translate.InvokeAsync(this._evaluatorKernel, new KernelArguments
                {
                    { "input", question.GetValue<string>() },
                    { "translate_to", language }
                }).ConfigureAwait(false);
            }

            return question.GetValue<string>();
        })!;
    }

    private async IAsyncEnumerable<TestSetItem> GetReasoningTestSetsAsync(
        IEnumerable<MemoryRecord[]> nodes,
        string language = null!,
        int retryCount = 3)
    {
        foreach (var partition in nodes)
        {
            var nodeText = string.Join(" ", partition.Select(r => r.GetPartitionText()));

            var seedQuestion = await this.GetQuestionSeedAsync(nodeText, language, retryCount).ConfigureAwait(false);

            var question = await this.GetReasoningQuestionAsync(nodeText, seedQuestion, language, retryCount).ConfigureAwait(false);

            var groundTruth = await this.GetQuestionAnswerAsync(nodeText, question, language, retryCount).ConfigureAwait(false);

            var testSet = new TestSetItem
            {
                Question = seedQuestion,
                QuestionType = QuestionType.Reasoning,
                GroundTruth = groundTruth.Answer,
                GroundTruthVerdict = groundTruth.Verdict,
                Context = partition.Select(r => r.GetPartitionText())
            };

            yield return testSet;
        }
    }

    private Task<string> GetReasoningQuestionAsync(string context, string seedQuestion, string language = null!, int retryCount = 3)
    {
        return this.Try(retryCount, async (remainingTry) =>
        {
            var question = await this.ReasoningQuestionGeneration.InvokeAsync(this._evaluatorKernel, new KernelArguments
            {
                { "question", seedQuestion },
                { "context", context }
            }).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(language))
            {
                question = await this.Translate.InvokeAsync(this._evaluatorKernel, new KernelArguments
                {
                    { "input", question.GetValue<string>() },
                    { "translate_to", language }
                }).ConfigureAwait(false);
            }

            return question.GetValue<string>();
        })!;
    }

    private async IAsyncEnumerable<TestSetItem> GetConditioningTestSetsAsync(
        IEnumerable<MemoryRecord[]> nodes,
        string language = null!,
        int retryCount = 3)
    {
        foreach (var partition in nodes)
        {
            var nodeText = string.Join(" ", partition.Select(r => r.GetPartitionText()));

            var seedQuestion = await this.GetQuestionSeedAsync(nodeText, language, retryCount).ConfigureAwait(false);

            var question = await this.GetConditioningQuestionAsync(nodeText, seedQuestion, language, retryCount).ConfigureAwait(false);

            var groundTruth = await this.GetQuestionAnswerAsync(nodeText, question, language, retryCount).ConfigureAwait(false);

            var testSet = new TestSetItem
            {
                Question = seedQuestion,
                QuestionType = QuestionType.Conditioning,
                GroundTruth = groundTruth.Answer,
                GroundTruthVerdict = groundTruth.Verdict,
                Context = partition.Select(r => r.GetPartitionText())
            };

            yield return testSet;
        }
    }

    private Task<string> GetConditioningQuestionAsync(string context, string seedQuestion, string language = null!, int retryCount = 3)
    {
        return this.Try(retryCount, async (remainingTry) =>
        {
            var question = await this.ConditioningQuestionGeneration.InvokeAsync(this._evaluatorKernel, new KernelArguments
            {
                { "question", seedQuestion },
                { "context", context }
            }).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(language))
            {
                question = await this.Translate.InvokeAsync(this._evaluatorKernel, new KernelArguments
                {
                    { "input", question.GetValue<string>() },
                    { "translate_to", language }
                }).ConfigureAwait(false);
            }

            return question.GetValue<string>();
        })!;
    }

    private async IAsyncEnumerable<TestSetItem> GetSimpleQuestionTestSetsAsync(
        IEnumerable<MemoryRecord[]> nodes,
        string language = null!,
        int retryCount = 3)
    {
        foreach (var partition in nodes)
        {
            var nodeText = string.Join(" ", partition.Select(r => r.GetPartitionText()));

            var seedQuestion = await this.GetQuestionSeedAsync(nodeText, language, retryCount).ConfigureAwait(false);

            var groundTruth = await this.GetQuestionAnswerAsync(nodeText, seedQuestion, language, retryCount).ConfigureAwait(false);

            var testSet = new TestSetItem
            {
                Question = seedQuestion,
                QuestionType = QuestionType.Simple,
                GroundTruth = groundTruth.Answer,
                GroundTruthVerdict = groundTruth.Verdict,
                Context = partition.Select(r => r.GetPartitionText())
            };

            yield return testSet;
        }
    }

    private Task<string> GetQuestionSeedAsync(
        string context,
        string language = null!,
        int retryCount = 3)
    {
        return this.Try(retryCount, async (remainingTry) =>
        {
            var phrases = await this.GetKeyPhrases(context, retryCount).ConfigureAwait(false);

            var seedQuestion = await this.SeedQuestionGeneration.InvokeAsync(this._evaluatorKernel, new KernelArguments
            {
                { "keyPhrase", phrases.First() },
                { "context", context }
            }).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(language))
            {
                seedQuestion = await this.Translate.InvokeAsync(this._evaluatorKernel, new KernelArguments
                {
                    { "input", seedQuestion.GetValue<string>() },
                    { "translate_to", language }
                }).ConfigureAwait(false);
            }

            return seedQuestion.GetValue<string>();
        })!;
    }

    private async Task<IEnumerable<string>> GetKeyPhrases(string context, int retryCount = 3)
    {
        var keyPhrases = await this.Try(retryCount, async (remainingTry) =>
        {
            var generatedKeyPhrases = await this.KeyPhraseExtraction.InvokeAsync(this._evaluatorKernel, new KernelArguments
            {
                { "input", context }
            }).ConfigureAwait(false);

            return JsonSerializer.Deserialize<StatementExtraction>(generatedKeyPhrases.GetValue<string>()!);
        }).ConfigureAwait(false);

        return this.Shuffle(keyPhrases!.Statements!);
    }

    private Task<QuestionAnswer> GetQuestionAnswerAsync(string context, string question, string language = null!, int retryCount = 3)
    {
        return this.Try(retryCount, async (remainingTry) =>
        {
            var generatedAnswer = await this.QuestionAnswerGeneration.InvokeAsync(this._evaluatorKernel, new KernelArguments
            {
                { "context", context },
                { "question", question }
            }).ConfigureAwait(false);

            var answer = JsonSerializer.Deserialize<QuestionAnswer>(generatedAnswer.GetValue<string>()!);

            if (answer!.Verdict <= 0 && remainingTry > 0)
            {
                throw new InvalidDataException();
            }

            if (!string.IsNullOrEmpty(language))
            {
                generatedAnswer = await this.Translate.InvokeAsync(this._evaluatorKernel, new KernelArguments
                {
                    { "input", answer.Answer },
                    { "translate_to", language }
                }).ConfigureAwait(false);

                answer.Answer = generatedAnswer.GetValue<string>()!;
            }

            return answer;
        });
    }
}
