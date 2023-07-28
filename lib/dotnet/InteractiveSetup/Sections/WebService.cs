// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.SemanticMemory.InteractiveSetup.Sections;

public static class WebService
{
    public static void Setup()
    {
        var enabled = true;
        SetupUI.AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "Enabled OpenAPI swagger doc at /swagger/index.html?",
            Options = new List<Answer>
            {
                new("Yes", () => { enabled = true; }),
                new("No", () => { enabled = false; }),
                new("-exit-", SetupUI.Exit),
            }
        });

        JObject data = AppSettings.Load();
        data[Main.MemKey]![Main.OpenApiEnabledKey] = enabled;
        AppSettings.Save(data);
    }
}
