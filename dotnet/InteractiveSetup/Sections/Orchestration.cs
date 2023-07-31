// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.SemanticMemory.InteractiveSetup.Sections;

public static class Orchestration
{
    public static void Setup()
    {
        SetupUI.AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "How should memory ingestion be orchestrated?",
            Options = new List<Answer>
            {
                new("Using asynchronous distributed queues (allows to mix handlers written in different languages)", DistributedOrchestrationSetup),
                new("In process orchestration, all .NET handlers run synchronously", InProcessOrchestrationSetup),
                new("-exit-", SetupUI.Exit),
            }
        });
    }

    private static void InProcessOrchestrationSetup()
    {
        JObject data = AppSettings.Load();

        data[Main.MemKey]![Main.OrchestrationKey] = new JObject();
        data[Main.MemKey]![Main.OrchestrationKey]![Main.TypeKey] = "InProcess";

        AppSettings.Save(data);
    }

    private static void DistributedOrchestrationSetup()
    {
        JObject data = AppSettings.Load();

        if (data[Main.MemKey]![Main.OrchestrationKey]![Main.DistributedPipelineKey] == null)
        {
            data[Main.MemKey]![Main.OrchestrationKey]![Main.DistributedPipelineKey] = new JObject();
        }

        data[Main.MemKey]![Main.OrchestrationKey]![Main.TypeKey] = "Distributed";

        var queuesType = "";
        SetupUI.AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "Which queue service will be used?",
            Options = new List<Answer>
            {
                new("Azure Queue", () => { queuesType = "AzureQueue"; }),
                new("RabbitMQ", () => { queuesType = "RabbitMQ"; }),
                new("-exit-", SetupUI.Exit),
            }
        });

        switch (queuesType)
        {
            case "AzureQueue":
            {
                data[Main.MemKey]![Main.OrchestrationKey]![Main.DistributedPipelineKey]!["RabbitMq"]?.Remove();
                data[Main.MemKey]![Main.OrchestrationKey]![Main.DistributedPipelineKey]!["AzureQueue"] = new JObject
                {
                    [Main.AuthKey] = Main.ConnectionStringAuthType,
                    [Main.AccountNameKey] = SetupUI.AskOpenQuestion("Azure Queue <account name>", data[Main.MemKey]![Main.OrchestrationKey]?[Main.DistributedPipelineKey]?["AzureQueue"]?[Main.AccountNameKey]?.ToString()),
                    [Main.ConnectionStringKey] = SetupUI.AskPassword("Azure Queue <connection string>", data[Main.MemKey]![Main.OrchestrationKey]?[Main.DistributedPipelineKey]?["AzureQueue"]?[Main.ConnectionStringKey]?.ToString())
                };
                break;
            }

            case "RabbitMQ":
            {
                data[Main.MemKey]![Main.OrchestrationKey]![Main.DistributedPipelineKey]!["AzureQueue"]?.Remove();
                data[Main.MemKey]![Main.OrchestrationKey]![Main.DistributedPipelineKey]!["RabbitMq"] = new JObject
                {
                    ["Host"] = SetupUI.AskOpenQuestion("RabbitMQ <host>", data[Main.MemKey]![Main.OrchestrationKey]?[Main.DistributedPipelineKey]?["RabbitMq"]?["Host"]?.ToString()),
                    ["Port"] = SetupUI.AskOpenQuestion("RabbitMQ <TCP port>", data[Main.MemKey]![Main.OrchestrationKey]?[Main.DistributedPipelineKey]?["RabbitMq"]?["Port"]?.ToString()),
                    ["Username"] = SetupUI.AskOpenQuestion("RabbitMQ <username>", data[Main.MemKey]![Main.OrchestrationKey]?[Main.DistributedPipelineKey]?["RabbitMq"]?["Username"]?.ToString()),
                    ["Password"] = SetupUI.AskPassword("RabbitMQ <password>", data[Main.MemKey]![Main.OrchestrationKey]?[Main.DistributedPipelineKey]?["RabbitMq"]?["Password"]?.ToString())
                };
                break;
            }

            default:
                throw new SetupException($"Unknown value {queuesType}");
        }

        AppSettings.Save(data);
    }
}
