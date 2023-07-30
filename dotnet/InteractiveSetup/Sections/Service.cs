// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.SemanticMemory.InteractiveSetup.Sections;

public static class Service
{
    public static void Setup()
    {
        SetupUI.AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "Run the web service (upload and search endpoints)?",
            Options = new List<Answer>
            {
                new("Yes", EnableWebService),
                new("No", DisableWebService),
                new("-exit-", SetupUI.Exit),
            }
        });

        SetupUI.AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "Run the .NET pipeline handlers?",
            Options = new List<Answer>
            {
                new("Yes", EnableHandlers),
                new("No", DisableHandlers),
                new("-exit-", SetupUI.Exit),
            }
        });
    }

    private static void EnableWebService()
    {
        JObject data = AppSettings.Load();
        data[Main.MemKey]!["Service"]!["RunWebService"] = true;
        AppSettings.Save(data);
    }

    private static void DisableWebService()
    {
        JObject data = AppSettings.Load();
        data[Main.MemKey]!["Service"]!["RunWebService"] = false;
        AppSettings.Save(data);
    }

    private static void EnableHandlers()
    {
        JObject data = AppSettings.Load();
        data[Main.MemKey]!["Service"]!["RunHandlers"] = true;
        AppSettings.Save(data);
    }

    private static void DisableHandlers()
    {
        JObject data = AppSettings.Load();
        data[Main.MemKey]!["Service"]!["RunHandlers"] = false;
        AppSettings.Save(data);
    }
}
