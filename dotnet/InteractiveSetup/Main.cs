// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.SemanticMemory.InteractiveSetup.Sections;
using Newtonsoft.Json.Linq;

namespace Microsoft.SemanticMemory.InteractiveSetup;

public static class Main
{
    public const string MemKey = "SemanticMemory";
    public const string StorageKey = "ContentStorage";
    public const string OrchestrationKey = "Orchestration";
    public const string HandlersKey = "Handlers";
    public const string DistributedPipelineKey = "DistributedPipeline";
    public const string AuthKey = "Auth";
    public const string TypeKey = "Type";
    public const string EndpointKey = "Endpoint";
    public const string ApiKeyKey = "APIKey";
    public const string VectorIndexPrefixKey = "VectorIndexPrefix";
    public const string ConnectionStringKey = "ConnectionString";
    public const string DeploymentNameKey = "Deployment";
    public const string ModelNameKey = "Model";
    public const string AccountNameKey = "Account";
    public const string ContainerNameKey = "Container";
    public const string SearchKey = "Search";
    public const string OpenApiEnabledKey = "OpenApiEnabled";

    public const string ConnectionStringAuthType = "ConnectionString";
    public const string AzureOpenAIType = "AzureOpenAI";
    public const string OpenAIType = "OpenAI";
    public const string AzureCognitiveSearchType = "AzureCognitiveSearch";

    public static void InteractiveSetup(
        bool cfgService = false,
        bool cfgContentStorage = true,
        bool cfgOrchestration = true,
        bool cfgHandlers = true,
        bool cfgWebService = true,
        bool cfgSearch = true,
        bool cfgLogging = true)
    {
        try
        {
            if (cfgService) { Service.Setup(); }
            else { Service.RemoveSettings(); }

            if (cfgService && cfgWebService) { WebService.Setup(); }

            if (cfgService && cfgSearch) { Search.Setup(); }

            if (cfgContentStorage) { ContentStorage.Setup(); }

            if (cfgOrchestration) { Orchestration.Setup(); }
            else { Orchestration.RemoveSettings(); }

            if (cfgHandlers) { PipelineHandlers.Setup(); }

            if (cfgLogging) { Logger.Setup(); }
        }
        catch (Exception e)
        {
            Console.WriteLine($"== Error: {e.GetType().FullName}");
            Console.WriteLine($"== {e.Message}");
        }

        SetupUI.Exit();
    }

    public static string? FindValueFor(string name)
    {
        static bool NotEmpty(JToken? token, out string value)
        {
            value = string.Empty;
            if (token != null && !string.IsNullOrEmpty(token.ToString()))
            {
                value = token.ToString();
                return true;
            }

            return false;
        }

        static bool IsEquals(JToken? token, string value)
        {
            return (token != null && string.Equals(token.ToString(), value, StringComparison.Ordinal));
        }

        JObject data = AppSettings.Load();
        switch (name)
        {
            case "AzureCognitiveSearchEndpoint":
            {
                foreach (JToken x in (JArray)(data[MemKey]![HandlersKey]!["save_embeddings"]!["VectorDbs"] ?? new JArray()))
                {
                    if (x.Type == JTokenType.Comment) { continue; }

                    if (IsEquals(x[TypeKey], AzureCognitiveSearchType) && NotEmpty(x[EndpointKey], out var value))
                    {
                        return value;
                    }
                }

                if (IsEquals(data[MemKey]![SearchKey]!["VectorDb"]![TypeKey], AzureCognitiveSearchType)
                    && NotEmpty(data[MemKey]![SearchKey]!["VectorDb"]![EndpointKey], out var value2))
                {
                    return value2;
                }

                break;
            }
            case "AzureCognitiveSearchApiKey":
            {
                foreach (JToken x in (JArray)(data[MemKey]![HandlersKey]!["save_embeddings"]!["VectorDbs"] ?? new JArray()))
                {
                    if (x.Type == JTokenType.Comment) { continue; }

                    if (IsEquals(x[TypeKey], AzureCognitiveSearchType) && NotEmpty(x[ApiKeyKey], out var value))
                    {
                        return value;
                    }
                }

                if (data[MemKey]![SearchKey]!["VectorDb"]![TypeKey]?.ToString() == AzureCognitiveSearchType
                    && NotEmpty(data[MemKey]![SearchKey]!["VectorDb"]![ApiKeyKey], out var value2))
                {
                    return value2;
                }

                break;
            }
            case "AzureCognitiveSearchIndexNamePrefix":
            {
                foreach (JToken x in (JArray)(data[MemKey]![HandlersKey]!["save_embeddings"]!["VectorDbs"] ?? new JArray()))
                {
                    if (x.Type == JTokenType.Comment) { continue; }

                    if (IsEquals(x[TypeKey], AzureCognitiveSearchType) && NotEmpty(x["VectorIndexPrefix"], out var value))
                    {
                        return value;
                    }
                }

                if (data[MemKey]![SearchKey]!["VectorDb"]![TypeKey]?.ToString() == AzureCognitiveSearchType
                    && NotEmpty(data[MemKey]![SearchKey]!["VectorDb"]!["VectorIndexPrefix"], out var value2))
                {
                    return value2;
                }

                break;
            }
            case "AzureOpenAIEndpoint":
            {
                foreach (JToken x in (JArray)(data[MemKey]![HandlersKey]!["gen_embeddings"]!["EmbeddingGenerators"] ?? new JArray()))
                {
                    if (x.Type == JTokenType.Comment) { continue; }

                    if (IsEquals(x[TypeKey], AzureOpenAIType) && NotEmpty(x[EndpointKey], out var value))
                    {
                        return value;
                    }
                }

                if (IsEquals(data[MemKey]![SearchKey]!["EmbeddingGenerator"]![TypeKey], AzureOpenAIType)
                    && NotEmpty(data[MemKey]![SearchKey]!["EmbeddingGenerator"]![EndpointKey], out var value2))
                {
                    return value2;
                }

                break;
            }
            case "AzureOpenAIEmbeddingDeployment":
            {
                foreach (JToken x in (JArray)(data[MemKey]![HandlersKey]!["gen_embeddings"]!["EmbeddingGenerators"] ?? new JArray()))
                {
                    if (x.Type == JTokenType.Comment) { continue; }

                    if (IsEquals(x[TypeKey], AzureOpenAIType) && NotEmpty(x[DeploymentNameKey], out var value))
                    {
                        return value;
                    }
                }

                if (IsEquals(data[MemKey]![SearchKey]!["EmbeddingGenerator"]![TypeKey], AzureOpenAIType)
                    && NotEmpty(data[MemKey]![SearchKey]!["EmbeddingGenerator"]![DeploymentNameKey], out var value2))
                {
                    return value2;
                }

                break;
            }
            case "AzureOpenAITextDeployment":
            {
                if (IsEquals(data[MemKey]![SearchKey]!["TextGenerator"]![TypeKey], AzureOpenAIType)
                    && NotEmpty(data[MemKey]![SearchKey]!["TextGenerator"]![DeploymentNameKey], out var value2))
                {
                    return value2;
                }

                break;
            }
            case "AzureOpenAIApiKey":
            {
                foreach (JToken x in (JArray)(data[MemKey]![HandlersKey]!["gen_embeddings"]!["EmbeddingGenerators"] ?? new JArray()))
                {
                    if (x.Type == JTokenType.Comment) { continue; }

                    if (IsEquals(x[TypeKey], AzureOpenAIType) && NotEmpty(x[ApiKeyKey], out var value))
                    {
                        return value;
                    }
                }

                if (IsEquals(data[MemKey]![SearchKey]!["EmbeddingGenerator"]![TypeKey], AzureOpenAIType)
                    && NotEmpty(data[MemKey]![SearchKey]!["EmbeddingGenerator"]![ApiKeyKey], out var value2))
                {
                    return value2;
                }

                break;
            }
            case "OpenAIEmbeddingModel":
            {
                foreach (JToken x in (JArray)(data[MemKey]![HandlersKey]!["gen_embeddings"]!["EmbeddingGenerators"] ?? new JArray()))
                {
                    if (x.Type == JTokenType.Comment) { continue; }

                    if (IsEquals(x[TypeKey], OpenAIType) && NotEmpty(x[ModelNameKey], out var value))
                    {
                        return value;
                    }
                }

                if (IsEquals(data[MemKey]![SearchKey]!["EmbeddingGenerator"]![TypeKey], OpenAIType)
                    && NotEmpty(data[MemKey]![SearchKey]!["EmbeddingGenerator"]![ModelNameKey], out var value2))
                {
                    return value2;
                }

                break;
            }
            case "OpenAITextModel":
            {
                if (IsEquals(data[MemKey]![SearchKey]!["TextGenerator"]![TypeKey], OpenAIType)
                    && NotEmpty(data[MemKey]![SearchKey]!["TextGenerator"]![ModelNameKey], out var value2))
                {
                    return value2;
                }

                break;
            }
            case "OpenAIApiKey":
            {
                foreach (JToken x in (JArray)(data[MemKey]![HandlersKey]!["gen_embeddings"]!["EmbeddingGenerators"] ?? new JArray()))
                {
                    if (x.Type == JTokenType.Comment) { continue; }

                    if (IsEquals(x[TypeKey], OpenAIType) && NotEmpty(x[ApiKeyKey], out var value))
                    {
                        return value;
                    }
                }

                if (IsEquals(data[MemKey]![SearchKey]!["EmbeddingGenerator"]![TypeKey], OpenAIType)
                    && NotEmpty(data[MemKey]![SearchKey]!["EmbeddingGenerator"]![ApiKeyKey], out var value2))
                {
                    return value2;
                }

                break;
            }
            case "OpenAIOrgId":
            {
                foreach (JToken x in (JArray)(data[MemKey]![HandlersKey]!["gen_embeddings"]!["EmbeddingGenerators"] ?? new JArray()))
                {
                    if (x.Type == JTokenType.Comment) { continue; }

                    if (IsEquals(x[TypeKey], OpenAIType) && NotEmpty(x["OrgId"], out var value))
                    {
                        return value;
                    }
                }

                if (IsEquals(data[MemKey]![SearchKey]!["EmbeddingGenerator"]![TypeKey], OpenAIType)
                    && NotEmpty(data[MemKey]![SearchKey]!["EmbeddingGenerator"]!["OrgId"], out var value2))
                {
                    return value2;
                }

                break;
            }

            default:
                throw new SetupException($"Unknown '{name}' case");
        }

        return string.Empty;
    }
}
