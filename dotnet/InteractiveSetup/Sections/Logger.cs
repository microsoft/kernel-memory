// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.SemanticMemory.InteractiveSetup.Sections;

public static class Logger
{
    public static void Setup()
    {
        string logLevel = "Debug";
        SetupUI.AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "Log level?",
            Options = new List<Answer>
            {
                new("Trace", () => { logLevel = "Trace"; }),
                new("Debug", () => { logLevel = "Debug"; }),
                new("Information", () => { logLevel = "Information"; }),
                new("Warning", () => { logLevel = "Warning"; }),
                new("Error", () => { logLevel = "Error"; }),
                new("Critical", () => { logLevel = "Critical"; }),
                new("-exit-", SetupUI.Exit),
            }
        });

        JObject data = AppSettings.Load();

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
        AppSettings.Save(data);
    }
}
