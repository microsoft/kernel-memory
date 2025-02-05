// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;

namespace Microsoft.KernelMemory.InteractiveSetup.Doctor;

public static class Services
{
    public static void CheckAndShow(KernelMemoryConfig config, HashSet<string> services, List<Tuple<string, string>> warnings, List<Tuple<string, string>> errors)
    {
        string title = "Services Dependencies";
        Console.WriteLine($"\n\u001b[1;37m### {title}\u001b[0m\n");

        if (services.Count == 0)
        {
            Console.WriteLine("None.");
            return;
        }

        foreach (var k in services)
        {
            switch (k)
            {
                default:
                {
                    Console.WriteLine($"- {k}");
                    break;
                }
                case "AzureAIContentSafety":
                case "AzureAIDocIntel":
                case "AzureAISearch":
                {
                    Console.WriteLine($"- {k}");
                    Console.WriteLine($"  - Auth: {config.Services[k]["Auth"]}");

                    string endpoint = (string)config.Services[k]["Endpoint"];
                    string auth = (string)config.Services[k]["Auth"];
                    string key = (string)config.Services[k]["APIKey"];
                    if (auth == "ApiKey" && string.IsNullOrWhiteSpace(key))
                    {
                        errors.Add(k, "API Key is not set");
                    }

                    if (string.IsNullOrEmpty(endpoint) || !Uri.TryCreate(endpoint, UriKind.Absolute, out _))
                    {
                        errors.Add(k, "Endpoint is not set or invalid");
                    }

                    break;
                }
                case "AzureBlobs":
                {
                    Console.WriteLine($"- {k}");
                    Console.WriteLine($"  - Auth: {config.Services[k]["Auth"]}");
                    Console.WriteLine($"  - Container: {config.Services[k]["Container"]}");

                    string auth = (string)config.Services[k]["Auth"];
                    string account = (string)config.Services[k]["Account"];
                    string cs = (string)config.Services[k]["ConnectionString"];
                    string container = (string)config.Services[k]["Container"];

                    if (auth == "AzureIdentity" && string.IsNullOrWhiteSpace(account))
                    {
                        errors.Add(k, "Account is not set");
                    }
                    else if (auth == "ConnectionString" && string.IsNullOrWhiteSpace(cs))
                    {
                        errors.Add(k, "ConnectionString is not set");
                    }

                    if (string.IsNullOrEmpty(container))
                    {
                        errors.Add(k, "Container is not set");
                    }

                    break;
                }
                case "AzureQueues":
                {
                    Console.WriteLine($"- {k}");
                    Console.WriteLine($"  - Auth: {config.Services[k]["Auth"]}");
                    Console.WriteLine($"  - PollDelayMsecs: {config.Services[k]["PollDelayMsecs"]}");
                    Console.WriteLine($"  - FetchBatchSize: {config.Services[k]["FetchBatchSize"]}");
                    Console.WriteLine($"  - FetchLockSeconds: {config.Services[k]["FetchLockSeconds"]}");
                    Console.WriteLine($"  - MaxRetriesBeforePoisonQueue: {config.Services[k]["MaxRetriesBeforePoisonQueue"]}");
                    Console.WriteLine($"  - PoisonQueueSuffix: {config.Services[k]["PoisonQueueSuffix"]}");

                    string auth = (string)config.Services[k]["Auth"];
                    string account = (string)config.Services[k]["Account"];
                    string cs = (string)config.Services[k]["ConnectionString"];

                    if (auth == "AzureIdentity" && string.IsNullOrWhiteSpace(account))
                    {
                        errors.Add(k, "Account is not set");
                    }
                    else if (auth == "ConnectionString" && string.IsNullOrWhiteSpace(cs))
                    {
                        errors.Add(k, "ConnectionString is not set");
                    }

                    break;
                }
                case "AzureOpenAIEmbedding":
                {
                    Console.WriteLine($"- {k}");
                    Console.WriteLine($"  - Auth: {config.Services[k]["Auth"]}");
                    Console.WriteLine($"  - Deployment: {config.Services[k]["Deployment"]}");
                    Console.WriteLine($"  - MaxTokenTotal: {config.Services[k]["MaxTokenTotal"]}");
                    Console.WriteLine($"  - Tokenizer: {config.Services[k]["Tokenizer"]}");
                    Console.WriteLine($"  - MaxEmbeddingBatchSize: {config.Services[k]["MaxEmbeddingBatchSize"]}");
                    Console.WriteLine($"  - MaxRetries: {config.Services[k]["MaxRetries"]}");

                    string endpoint = (string)config.Services[k]["Endpoint"];
                    string auth = (string)config.Services[k]["Auth"];
                    string key = (string)config.Services[k]["APIKey"];
                    if (auth == "ApiKey" && string.IsNullOrWhiteSpace(key))
                    {
                        errors.Add(k, "API Key is not set");
                    }

                    if (string.IsNullOrEmpty(endpoint) || !Uri.TryCreate(endpoint, UriKind.Absolute, out _))
                    {
                        errors.Add(k, "Endpoint is not set or invalid");
                    }

                    break;
                }
                case "AzureOpenAIText":
                {
                    Console.WriteLine($"- {k}");
                    Console.WriteLine($"  - Auth: {config.Services[k]["Auth"]}");
                    Console.WriteLine($"  - Deployment: {config.Services[k]["Deployment"]}");
                    Console.WriteLine($"  - MaxTokenTotal: {config.Services[k]["MaxTokenTotal"]}");
                    Console.WriteLine($"  - Tokenizer: {config.Services[k]["Tokenizer"]}");
                    Console.WriteLine($"  - MaxRetries: {config.Services[k]["MaxRetries"]}");

                    string endpoint = (string)config.Services[k]["Endpoint"];
                    string auth = (string)config.Services[k]["Auth"];
                    string key = (string)config.Services[k]["APIKey"];
                    if (auth == "ApiKey" && string.IsNullOrWhiteSpace(key))
                    {
                        errors.Add(k, "API Key is not set");
                    }

                    if (string.IsNullOrEmpty(endpoint) || !Uri.TryCreate(endpoint, UriKind.Absolute, out _))
                    {
                        errors.Add(k, "Endpoint is not set or invalid");
                    }

                    break;
                }
                case "SimpleFileStorage":
                case "SimpleQueues":
                case "SimpleVectorDb":
                {
                    Console.WriteLine($"- {k}");
                    Console.WriteLine($"  - StorageType: {config.Services[k]["StorageType"]}");
                    break;
                }
                case "OpenAI":
                {
                    var openAIEmbeddings = config.DataIngestion.EmbeddingGenerationEnabled && config.DataIngestion.EmbeddingGeneratorTypes.Contains("OpenAI");

                    Console.WriteLine($"- {k}");
                    Console.WriteLine($"  - TextModel: {config.Services[k]["TextModel"]}");
                    Console.WriteLine($"  - TextModelMaxTokenTotal: {config.Services[k]["TextModelMaxTokenTotal"]}");
                    if (openAIEmbeddings)
                    {
                        Console.WriteLine($"  - EmbeddingModel: {config.Services[k]["EmbeddingModel"]}");
                        Console.WriteLine($"  - EmbeddingModelMaxTokenTotal: {config.Services[k]["EmbeddingModelMaxTokenTotal"]}");
                    }

                    Console.WriteLine($"  - MaxRetries: {config.Services[k]["MaxRetries"]}");

                    string key = (string)config.Services[k]["APIKey"];
                    string textModel = (string)config.Services[k]["TextModel"];
                    if (string.IsNullOrWhiteSpace(key))
                    {
                        errors.Add(k, "API Key is not set");
                    }

                    if (string.IsNullOrWhiteSpace(textModel))
                    {
                        warnings.Add(k, "TextModel is not set");
                    }

                    if (openAIEmbeddings)
                    {
                        string embeddingModel = (string)config.Services[k]["EmbeddingModel"];
                        if (string.IsNullOrWhiteSpace(embeddingModel))
                        {
                            errors.Add(k, "EmbeddingModel is not set");
                        }
                    }

                    break;
                }
            }
        }
    }
}
