// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.SemanticMemory.InteractiveSetup.Sections;

public static class PipelineHandlers
{
    public static void Setup()
    {
        HandlersEmbeddingGeneratorsSetup();
        HandlersVectorDbsSetup();
    }

    private static void HandlersEmbeddingGeneratorsSetup()
    {
        JObject data = AppSettings.Load();

        var embeddingGeneratorType = "";
        SetupUI.AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "Which AI service will generate embeddings?",
            Options = new List<Answer>
            {
                new("Azure OpenAI", () => { embeddingGeneratorType = Main.AzureOpenAIType; }),
                new("OpenAI", () => { embeddingGeneratorType = Main.OpenAIType; }),
                new("-exit-", SetupUI.Exit),
            }
        });

        switch (embeddingGeneratorType)
        {
            case Main.AzureOpenAIType:
            {
                data[Main.MemKey]![Main.HandlersKey]!["gen_embeddings"]!["EmbeddingGenerators"] = new JArray
                {
                    new JObject
                    {
                        [Main.TypeKey] = Main.AzureOpenAIType,
                        [Main.EndpointKey] = SetupUI.AskOpenQuestion("Azure OpenAI <endpoint>", Main.FindValueFor("AzureOpenAIEndpoint")),
                        [Main.DeploymentNameKey] = SetupUI.AskOpenQuestion("Azure OpenAI <deployment name>", Main.FindValueFor("AzureOpenAIEmbeddingDeployment")),
                        [Main.ApiKeyKey] = SetupUI.AskPassword("Azure OpenAI <API Key>", Main.FindValueFor("AzureOpenAIApiKey"))
                    }
                };
                break;
            }

            case Main.OpenAIType:
            {
                data[Main.MemKey]![Main.HandlersKey]!["gen_embeddings"]!["EmbeddingGenerators"] = new JArray
                {
                    new JObject
                    {
                        [Main.TypeKey] = Main.OpenAIType,
                        [Main.ModelNameKey] = SetupUI.AskOpenQuestion("OpenAI <embedding model name>", Main.FindValueFor("OpenAIEmbeddingModel")),
                        [Main.ApiKeyKey] = SetupUI.AskPassword("OpenAI <API Key>", Main.FindValueFor("OpenAIApiKey")),
                    }
                };
                break;
            }

            default:
                throw new SetupException($"Unknown value {embeddingGeneratorType}");
        }

        AppSettings.Save(data);
    }

    private static void HandlersVectorDbsSetup()
    {
        JObject data = AppSettings.Load();

        var vectorDbType = "";
        SetupUI.AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "Which vector DB service will store the embeddings?",
            Options = new List<Answer>
            {
                new("Azure Cognitive Search", () => { vectorDbType = Main.AzureCognitiveSearchType; }),
                new("-exit-", SetupUI.Exit),
            }
        });

        switch (vectorDbType)
        {
            case Main.AzureCognitiveSearchType:
            {
                data[Main.MemKey]![Main.HandlersKey]!["save_embeddings"]!["VectorDbs"] = new JArray
                {
                    new JObject
                    {
                        [Main.TypeKey] = Main.AzureCognitiveSearchType,
                        [Main.EndpointKey] = SetupUI.AskOpenQuestion("Azure Cognitive Search <endpoint>", Main.FindValueFor("AzureCognitiveSearchEndpoint")),
                        [Main.ApiKeyKey] = SetupUI.AskPassword("Azure Cognitive Search <API Key>", Main.FindValueFor("AzureCognitiveSearchApiKey")),
                        [Main.VectorIndexPrefixKey] = SetupUI.AskOpenQuestion("Optional index name prefix, e.g. 'smemory'", Main.FindValueFor("AzureCognitiveSearchIndexNamePrefix")),
                    }
                };
                break;
            }

            default:
                throw new SetupException($"Unknown value {vectorDbType}");
        }

        AppSettings.Save(data);
    }
}
