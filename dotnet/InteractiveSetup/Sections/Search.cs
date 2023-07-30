// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.SemanticMemory.InteractiveSetup.Sections;

public static class Search
{
    public static void Setup()
    {
        SearchVectorDbSetup();
        SearchEmbeddingGeneratorSetup();
        SearchTextGeneratorSetup();
    }

    private static void SearchVectorDbSetup()
    {
        JObject data = AppSettings.Load();

        var vectorDbType = "";
        SetupUI.AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "When searching for answers, which vector DB service contains embeddings to search?",
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
                data[Main.MemKey]![Main.SearchKey]!["VectorDb"] = new JObject
                {
                    [Main.TypeKey] = Main.AzureCognitiveSearchType,
                    [Main.EndpointKey] = SetupUI.AskOpenQuestion("Azure Cognitive Search <endpoint>", Main.FindValueFor("AzureCognitiveSearchEndpoint")),
                    [Main.ApiKeyKey] = SetupUI.AskPassword("Azure Cognitive Search <API Key>", Main.FindValueFor("AzureCognitiveSearchApiKey"))
                };
                break;
            }

            default:
                throw new SetupException($"Unknown value {vectorDbType}");
        }

        AppSettings.Save(data);
    }

    private static void SearchEmbeddingGeneratorSetup()
    {
        JObject data = AppSettings.Load();

        var embeddingGeneratorType = "";
        SetupUI.AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "When searching for answers, which embedding generator should we use for the question?",
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
                data[Main.MemKey]![Main.SearchKey]!["EmbeddingGenerator"] = new JObject
                {
                    [Main.TypeKey] = Main.AzureOpenAIType,
                    [Main.EndpointKey] = SetupUI.AskOpenQuestion("Azure OpenAI <endpoint>", Main.FindValueFor("AzureOpenAIEndpoint")),
                    [Main.DeploymentNameKey] = SetupUI.AskOpenQuestion("Azure OpenAI <deployment name>", Main.FindValueFor("AzureOpenAIEmbeddingDeployment")),
                    [Main.ApiKeyKey] = SetupUI.AskPassword("Azure OpenAI <API Key>", Main.FindValueFor("AzureOpenAIApiKey"))
                };
                break;
            }

            case Main.OpenAIType:
            {
                data[Main.MemKey]![Main.SearchKey]!["EmbeddingGenerator"] = new JObject
                {
                    [Main.TypeKey] = Main.OpenAIType,
                    [Main.ModelNameKey] = SetupUI.AskOpenQuestion("OpenAI <embedding model name>", Main.FindValueFor("OpenAIEmbeddingModel")),
                    [Main.ApiKeyKey] = SetupUI.AskPassword("OpenAI <API Key>", Main.FindValueFor("OpenAIApiKey")),
                };
                break;
            }

            default:
                throw new SetupException($"Unknown value {embeddingGeneratorType}");
        }

        AppSettings.Save(data);
    }

    private static void SearchTextGeneratorSetup()
    {
        JObject data = AppSettings.Load();

        var textGeneratorType = "";
        SetupUI.AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "When generating answers, which LLM text generator should be used?",
            Options = new List<Answer>
            {
                new("Azure OpenAI", () => { textGeneratorType = Main.AzureOpenAIType; }),
                new("OpenAI", () => { textGeneratorType = Main.OpenAIType; }),
                new("-exit-", SetupUI.Exit),
            }
        });

        switch (textGeneratorType)
        {
            case Main.AzureOpenAIType:
            {
                data[Main.MemKey]![Main.SearchKey]!["TextGenerator"] = new JObject
                {
                    [Main.TypeKey] = Main.AzureOpenAIType,
                    [Main.EndpointKey] = SetupUI.AskOpenQuestion("Azure OpenAI <endpoint>", Main.FindValueFor("AzureOpenAIEndpoint")),
                    [Main.DeploymentNameKey] = SetupUI.AskOpenQuestion("Azure OpenAI <deployment name>", Main.FindValueFor("AzureOpenAITextDeployment")),
                    [Main.ApiKeyKey] = SetupUI.AskPassword("Azure OpenAI <API Key>", Main.FindValueFor("AzureOpenAIApiKey"))
                };
                break;
            }

            case Main.OpenAIType:
            {
                data[Main.MemKey]![Main.SearchKey]!["TextGenerator"] = new JObject
                {
                    [Main.TypeKey] = Main.OpenAIType,
                    [Main.ModelNameKey] = SetupUI.AskOpenQuestion("OpenAI <model name>", Main.FindValueFor("OpenAITextModel")),
                    [Main.ApiKeyKey] = SetupUI.AskPassword("OpenAI <API Key>", Main.FindValueFor("OpenAIApiKey")),
                };
                break;
            }

            default:
                throw new SetupException($"Unknown value {textGeneratorType}");
        }

        AppSettings.Save(data);
    }
}
