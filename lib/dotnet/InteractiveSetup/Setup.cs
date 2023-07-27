// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.SemanticKernel.SemanticMemory.InteractiveSetup;

public static class Setup
{
    private const string SettingsFile = "appsettings.Development.json";

    private const string MemKey = "SKMemory";
    private const string StorageKey = "ContentStorage";
    private const string OrchestrationKey = "Orchestration";
    private const string HandlersKey = "Handlers";
    private const string DistributedPipelineKey = "DistributedPipeline";
    private const string AuthKey = "Auth";
    private const string TypeKey = "Type";
    private const string EndpointKey = "Endpoint";
    private const string ApiKeyKey = "APIKey";
    private const string ConnectionStringKey = "ConnectionString";
    private const string DeploymentNameKey = "Deployment";
    private const string ModelNameKey = "Model";
    private const string AccountNameKey = "Account";
    private const string ContainerNameKey = "Container";

    private const string ConnectionStringAuthType = "ConnectionString";
    private const string AzureOpenAIType = "AzureOpenAI";
    private const string OpenAIType = "OpenAI";

    public static void InteractiveSetup(
        bool cfgContentStorage = true,
        bool cfgOrchestration = true,
        bool cfgHandlers = true,
        bool cfgWebService = true,
        bool cfgLogging = true)
    {
        try
        {
            if (cfgContentStorage) { ContentStorageSetup(); }

            if (cfgOrchestration) { OrchestrationSetup(); }

            if (cfgHandlers) { HandlersSetup(); }

            if (cfgWebService) { WebServiceSetup(); }

            if (cfgLogging) { LoggerSetup(); }
        }
        catch (Exception e)
        {
            Console.WriteLine($"== Error: {e.GetType().FullName}");
            Console.WriteLine($"== {e.Message}");
            Exit();
        }
    }

    private static void ContentStorageSetup()
    {
        AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "Where should the service store files?",
            Options = new List<Option>
            {
                new("Azure Blobs", AzureBlobContentStorageSetup),
                new("Local file system", FileSystemContentStorageSetup),
                new("-exit-", Exit),
            }
        });
    }

    private static void AzureBlobContentStorageSetup()
    {
        JObject data = ReadySKMemoryConfig();

        data[MemKey]![StorageKey] = new JObject()
        {
            [TypeKey] = "AzureBlobs",
            ["AzureBlobs"] = new JObject
            {
                [ContainerNameKey] = AskOpenQuestion("Azure Blobs <container name>", data[MemKey]?[StorageKey]?["AzureBlobs"]?[ContainerNameKey]?.ToString()),
                [AccountNameKey] = AskOpenQuestion("Azure Blobs <account name>", data[MemKey]?[StorageKey]?["AzureBlobs"]?[AccountNameKey]?.ToString()),
                [ConnectionStringKey] = AskPassword("Azure Blobs <connection string>", data[MemKey]?[StorageKey]?["AzureBlobs"]?[ConnectionStringKey]?.ToString()),
                [AuthKey] = ConnectionStringAuthType
            }
        };

        WriteJsonFile(SettingsFile, data);
    }

    private static void FileSystemContentStorageSetup()
    {
        JObject data = ReadySKMemoryConfig();

        data[MemKey]![StorageKey] = new JObject
        {
            [TypeKey] = "FileSystem",
            ["FileSystem"] = new JObject
            {
                ["Directory"] = AskOpenQuestion("Directory", data[MemKey]?[StorageKey]?["FileSystem"]?["Directory"]?.ToString())
            }
        };

        string path = data[MemKey]?[StorageKey]?["FileSystem"]?["Directory"]?.ToString()!;

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            if (!Directory.Exists(path))
            {
                throw new SetupException($"Unable to find/create directory {path}");
            }
        }

        WriteJsonFile(SettingsFile, data);
    }

    private static void OrchestrationSetup()
    {
        AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "How should memory ingestion be orchestrated?",
            Options = new List<Option>
            {
                new("In process orchestration, all .NET handlers run synchronously", InProcessOrchestrationSetup),
                new("Using asynchronous distributed queues (allows to mix handlers written in different languages)", DistributedOrchestrationSetup),
                new("-exit-", Exit),
            }
        });
    }

    private static void InProcessOrchestrationSetup()
    {
        JObject data = ReadySKMemoryConfig();

        data[MemKey]![OrchestrationKey] = new JObject();
        data[MemKey]![OrchestrationKey]![TypeKey] = "InProcess";

        WriteJsonFile(SettingsFile, data);
    }

    private static void DistributedOrchestrationSetup()
    {
        JObject data = ReadySKMemoryConfig();

        if (data[MemKey]![OrchestrationKey]![DistributedPipelineKey] == null)
        {
            data[MemKey]![OrchestrationKey]![DistributedPipelineKey] = new JObject();
        }

        data[MemKey]![OrchestrationKey]![TypeKey] = "Distributed";

        var queuesType = "";
        AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "Which queue service will be used?",
            Options = new List<Option>
            {
                new("Azure Queue", () => { queuesType = "AzureQueue"; }),
                new("RabbitMQ", () => { queuesType = "RabbitMQ"; }),
                new("-exit-", Exit),
            }
        });

        switch (queuesType)
        {
            case "AzureQueue":
            {
                data[MemKey]![OrchestrationKey]![DistributedPipelineKey]!["RabbitMq"]?.Remove();
                data[MemKey]![OrchestrationKey]![DistributedPipelineKey]!["AzureQueue"] = new JObject
                {
                    [AuthKey] = ConnectionStringAuthType,
                    [AccountNameKey] = AskOpenQuestion("Azure Queue <account name>", data[MemKey]![OrchestrationKey]?[DistributedPipelineKey]?["AzureQueue"]?[AccountNameKey]?.ToString()),
                    [ConnectionStringKey] = AskPassword("Azure Queue <connection string>", data[MemKey]![OrchestrationKey]?[DistributedPipelineKey]?["AzureQueue"]?[ConnectionStringKey]?.ToString())
                };
                break;
            }

            case "RabbitMQ":
            {
                data[MemKey]![OrchestrationKey]![DistributedPipelineKey]!["AzureQueue"]?.Remove();
                data[MemKey]![OrchestrationKey]![DistributedPipelineKey]!["RabbitMq"] = new JObject
                {
                    ["Host"] = AskOpenQuestion("RabbitMQ <host>", data[MemKey]![OrchestrationKey]?[DistributedPipelineKey]?["RabbitMq"]?["Host"]?.ToString()),
                    ["Port"] = AskOpenQuestion("RabbitMQ <TCP port>", data[MemKey]![OrchestrationKey]?[DistributedPipelineKey]?["RabbitMq"]?["Port"]?.ToString()),
                    ["Username"] = AskOpenQuestion("RabbitMQ <username>", data[MemKey]![OrchestrationKey]?[DistributedPipelineKey]?["RabbitMq"]?["Username"]?.ToString()),
                    ["Password"] = AskPassword("RabbitMQ <password>", data[MemKey]![OrchestrationKey]?[DistributedPipelineKey]?["RabbitMq"]?["Password"]?.ToString())
                };
                break;
            }

            default:
                throw new SetupException($"Unknown value {queuesType}");
        }

        WriteJsonFile(SettingsFile, data);
    }

    private static void HandlersSetup()
    {
        EmbeddingGeneratorsSetup();
        EmbeddingStorageSetup();
    }

    private static void EmbeddingGeneratorsSetup()
    {
        JObject data = ReadySKMemoryConfig();

        var embeddingsType = "";
        AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "Which AI service will generate embeddings?",
            Options = new List<Option>
            {
                new("Azure OpenAI", () => { embeddingsType = AzureOpenAIType; }),
                new("OpenAI", () => { embeddingsType = OpenAIType; }),
                new("-exit-", Exit),
            }
        });

        switch (embeddingsType)
        {
            case AzureOpenAIType:
            {
                // Read current values
                string? deployment = string.Empty, endpoint = string.Empty, apiKey = string.Empty;
                foreach (JToken x in (JArray)(data[MemKey]![HandlersKey]!["gen_embeddings"]!["EmbeddingGenerators"] ?? new JArray()))
                {
                    if (x[TypeKey]?.ToString() == AzureOpenAIType)
                    {
                        endpoint = x[EndpointKey]?.ToString();
                        deployment = x[DeploymentNameKey]?.ToString();
                        apiKey = x[ApiKeyKey]?.ToString();
                    }
                }

                data[MemKey]![HandlersKey]!["gen_embeddings"]!["EmbeddingGenerators"] = new JArray
                {
                    new JObject
                    {
                        [TypeKey] = AzureOpenAIType,
                        [EndpointKey] = AskOpenQuestion("Azure OpenAI <endpoint>", endpoint),
                        [DeploymentNameKey] = AskOpenQuestion("Azure OpenAI <deployment name>", deployment),
                        [ApiKeyKey] = AskPassword("Azure OpenAI <API Key>", apiKey)
                    }
                };
                break;
            }

            case OpenAIType:
            {
                // Read current values
                string? model = string.Empty, apiKey = string.Empty;
                foreach (JToken x in (JArray)(data[MemKey]![HandlersKey]!["gen_embeddings"]!["EmbeddingGenerators"] ?? new JArray()))
                {
                    if (x[TypeKey]?.ToString() == OpenAIType)
                    {
                        model = x[ModelNameKey]?.ToString();
                        apiKey = x[ApiKeyKey]?.ToString();
                    }
                }

                data[MemKey]![HandlersKey]!["gen_embeddings"]!["EmbeddingGenerators"] = new JArray
                {
                    new JObject
                    {
                        [TypeKey] = OpenAIType,
                        [ModelNameKey] = AskOpenQuestion("OpenAI <embedding model name>", model),
                        [ApiKeyKey] = AskPassword("OpenAI <API Key>", apiKey)
                    }
                };
                break;
            }

            default:
                throw new SetupException($"Unknown value {embeddingsType}");
        }

        WriteJsonFile(SettingsFile, data);
    }

    private static void EmbeddingStorageSetup()
    {
        JObject data = ReadySKMemoryConfig();

        var embeddingsType = "";
        AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "Which vector DB service will store the embeddings?",
            Options = new List<Option>
            {
                new("Azure Cognitive Search", () => { embeddingsType = "AzureCognitiveSearch"; }),
                new("-exit-", Exit),
            }
        });

        switch (embeddingsType)
        {
            case "AzureCognitiveSearch":
            {
                // Read current values
                string? endpoint = string.Empty, apiKey = string.Empty;
                foreach (JToken x in (JArray)(data[MemKey]![HandlersKey]!["save_embeddings"]!["VectorDbs"] ?? new JArray()))
                {
                    if (x[TypeKey]?.ToString() == "AzureCognitiveSearch")
                    {
                        endpoint = x[EndpointKey]?.ToString();
                        apiKey = x[ApiKeyKey]?.ToString();
                    }
                }

                data[MemKey]![HandlersKey]!["save_embeddings"]!["VectorDbs"] = new JArray
                {
                    new JObject
                    {
                        [TypeKey] = "AzureCognitiveSearch",
                        [EndpointKey] = AskOpenQuestion("Azure Cognitive Search <endpoint>", endpoint),
                        [ApiKeyKey] = AskPassword("Azure Cognitive Search <API Key>", apiKey)
                    }
                };
                break;
            }

            default:
                throw new SetupException($"Unknown value {embeddingsType}");
        }

        WriteJsonFile(SettingsFile, data);
    }

    private static void WebServiceSetup()
    {
        var enabled = true;
        AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "Enabled OpenAPI swagger doc at /swagger/index.html?",
            Options = new List<Option>
            {
                new("Yes", () => { enabled = true; }),
                new("No", () => { enabled = false; }),
                new("-exit-", Exit),
            }
        });

        JObject data = ReadySKMemoryConfig();
        data[MemKey]!["OpenApiEnabled"] = enabled;
        WriteJsonFile(SettingsFile, data);
    }

    private static void LoggerSetup()
    {
        string logLevel = "Debug";
        AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "Log level?",
            Options = new List<Option>
            {
                new("Trace", () => { logLevel = "Trace"; }),
                new("Debug", () => { logLevel = "Debug"; }),
                new("Information", () => { logLevel = "Information"; }),
                new("Warning", () => { logLevel = "Warning"; }),
                new("Error", () => { logLevel = "Error"; }),
                new("Critical", () => { logLevel = "Critical"; }),
                new("-exit-", Exit),
            }
        });

        JObject data = ReadySKMemoryConfig();

        if (data["Logging"] == null)
        {
            data["Logging"] = new JObject();
        }

        if (data["Logging"]!["LogLevel"] == null)
        {
            data["Logging"]!["LogLevel"] = new JObject
            {
                ["Microsoft.AspNetCore"] = "Warning"
            };
        }

        data["Logging"]!["LogLevel"]!["Default"] = logLevel;
        WriteJsonFile(SettingsFile, data);
    }

    private static void Exit()
    {
        Environment.Exit(0);
    }

    #region JSON

    private static JObject ReadySKMemoryConfig()
    {
        CreateFileIfNotExists(SettingsFile);
        JObject data = ReadJsonFile(SettingsFile);

        if (data[MemKey] == null)
        {
            data[MemKey] = new JObject();
        }

        if (data[MemKey]![StorageKey] == null)
        {
            data[MemKey]![StorageKey] = new JObject();
        }

        if (data[MemKey]![OrchestrationKey] == null)
        {
            data[MemKey]![OrchestrationKey] = new JObject();
        }

        if (data[MemKey]![HandlersKey] == null)
        {
            data[MemKey]![HandlersKey] = new JObject();
        }

        if (data[MemKey]![HandlersKey]!["gen_embeddings"] == null)
        {
            data[MemKey]![HandlersKey]!["gen_embeddings"] = new JObject();
        }

        if (data[MemKey]![HandlersKey]!["save_embeddings"] == null)
        {
            data[MemKey]![HandlersKey]!["save_embeddings"] = new JObject();
        }

        return data;
    }

    private static JObject ReadJsonFile(string file)
    {
        if (!File.Exists(file))
        {
            throw new SetupException($"{file} not found");
        }

        string json = File.ReadAllText(SettingsFile);
        if (string.IsNullOrEmpty(json))
        {
            return new JObject();
        }

        var data = JsonConvert.DeserializeObject<JObject>(json);
        if (data == null)
        {
            throw new SetupException($"Unable to parse JSON file {file}");
        }

        return data;
    }

    private static void CreateFileIfNotExists(string file)
    {
        if (!File.Exists(file))
        {
            File.Create(file).Dispose();
            File.WriteAllText(file, "{}");
        }
    }

    private static void WriteJsonFile(string file, JObject data)
    {
        var json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(file, json);
    }

    #endregion

    #region UI

    private static string AskPassword(string question, string? defaultValue, bool trim = true, bool optional = false)
    {
        return AskOpenQuestion(question: question, defaultValue: defaultValue, trim: trim, optional: optional, isPassword: true);
    }

    private static string AskOpenQuestion(string question, string? defaultValue, bool trim = true, bool optional = false, bool isPassword = false)
    {
        if (!string.IsNullOrEmpty(defaultValue))
        {
            question = isPassword ? $"{question} [current: ****hidden****]" : $"{question} [current: {defaultValue}]";
        }

        question = isPassword ? $"{question} (value will not appear): " : $"{question}: ";

        string answer = string.Empty;
        var done = false;
        while (!done)
        {
            // Console.Clear();
            // Console.WriteLine(question);
            // var newAnswer = Console.ReadLine();
            string? newAnswer;
            if (isPassword)
            {
                newAnswer = ReadLine.ReadPassword(question);
                if (string.IsNullOrEmpty(newAnswer))
                {
                    newAnswer = defaultValue;
                }
            }
            else
            {
                newAnswer = ReadLine.Read(question, defaultValue);
            }

            answer = trim ? $"{newAnswer}".Trim() : $"{newAnswer}";

            done = (optional || !string.IsNullOrEmpty(answer));
        }

        return answer;
    }

    private static void AskQuestionWithOptions(QuestionWithOptions question)
    {
        void ShowQuestion(int selected)
        {
            Console.Clear();
            Console.WriteLine($"{question.Title}\n");
            for (int index = 0; index < question.Options.Count; index++)
            {
                Option option = question.Options[index];
                if (index == selected)
                {
                    Console.Write("> * ");
                }
                else
                {
                    Console.Write("  * ");
                }

                Console.WriteLine(option.Name);
            }
        }

        int current = 0;
        ShowQuestion(current);

        var maxPos = question.Options.Count - 1;
        var done = false;
        Action? action = null;
        while (!done)
        {
            // Always redraw, to take care of screen artifacts caused by keys pressed
            ShowQuestion(current);

            ConsoleKeyInfo pressedKey = Console.ReadKey();
            switch (pressedKey.Key)
            {
                // Move down
                case ConsoleKey.DownArrow:
                case ConsoleKey.PageDown:
                case ConsoleKey.Tab:
                case ConsoleKey.Spacebar:
                    if (current < maxPos) { current++; }

                    break;

                // Move up
                case ConsoleKey.UpArrow:
                case ConsoleKey.PageUp:
                case ConsoleKey.Backspace:
                    if (current > 0) { current--; }

                    break;

                // Reset
                case ConsoleKey.Home:
                case ConsoleKey.Clear:
                    current = 0;
                    break;

                // Go to end
                case ConsoleKey.End:
                    current = maxPos;
                    break;

                // Select current
                case ConsoleKey.Enter:
                    action = question.Options[current].Selected;
                    done = true;
                    break;

                // Exit
                case ConsoleKey.Escape:
                    action = Exit;
                    done = true;
                    break;
            }
        }

        Console.WriteLine();
        action?.Invoke();
    }

    private sealed class QuestionWithOptions
    {
        public string Title { get; set; } = string.Empty;
        public List<Option> Options { get; set; } = new();
    }

    private sealed class Option
    {
        public string Name { get; }
        public Action Selected { get; }

        public Option(string name, Action selected)
        {
            this.Name = name;
            this.Selected = selected;
        }
    }

    #endregion
}
