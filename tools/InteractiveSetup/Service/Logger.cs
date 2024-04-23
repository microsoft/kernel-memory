// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.KernelMemory.InteractiveSetup.UI;
using Newtonsoft.Json.Linq;

namespace Microsoft.KernelMemory.InteractiveSetup.Service;

internal static class Logger
{
    public static void Setup()
    {
        string logLevel = "Debug";
        SetupUI.AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "Log level?",
            Options = new List<Answer>
            {
                new("Trace", false, () => { logLevel = "Trace"; }),
                new("Debug", false, () => { logLevel = "Debug"; }),
                new("Information", false, () => { logLevel = "Information"; }),
                new("Warning", true, () => { logLevel = "Warning"; }),
                new("Error", false, () => { logLevel = "Error"; }),
                new("Critical", false, () => { logLevel = "Critical"; }),
                new("-exit-", false, SetupUI.Exit),
            }
        });

        AppSettings.GlobalChange(data =>
        {
            if (data["Logging"] == null) { data["Logging"] = new JObject(); }

            if (data["Logging"]!["LogLevel"] == null)
            {
                data["Logging"]!["LogLevel"] = new JObject { ["Microsoft.AspNetCore"] = "Warning" };
            }

            data["Logging"]!["LogLevel"]!["Default"] = logLevel;
        });
    }
}
