// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.KernelMemory.InteractiveSetup.UI;

namespace Microsoft.KernelMemory.InteractiveSetup.Services;

internal static class AzureAISearch
{
    public static void Setup(Context ctx, bool force = false)
    {
        if (!ctx.CfgAzureAISearch.Value && !force) { return; }

        ctx.CfgAzureAISearch.Value = false;
        const string ServiceName = "AzureAISearch";

        if (!AppSettings.GetCurrentConfig().Services.TryGetValue(ServiceName, out var config))
        {
            config = new Dictionary<string, object>
            {
                { "Endpoint", "" },
                { "Auth", "ApiKey" },
                { "APIKey", "" },
                { "UseHybridSearch", false },
                { "UseStickySessions", false }
            };
            AppSettings.AddService(ServiceName, config);
        }

        SetupUI.AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = $"[{ServiceName}] Which type of authentication do you want to use?",
            Options =
            [
                new("Azure Identity (Entra)", config["Auth"].ToString() == "AzureIdentity", () => AppSettings.Change(x =>
                {
                    x.Services[ServiceName]["Auth"] = "AzureIdentity";
                    x.Services[ServiceName].Remove("APIKey");
                })),
                new("API Key", config["Auth"].ToString() != "AzureIdentity", () => AppSettings.Change(x =>
                {
                    x.Services[ServiceName]["Auth"] = "ApiKey";
                    x.Services[ServiceName]["APIKey"] = SetupUI.AskPassword("Azure AI Search <API Key>", config["APIKey"].ToString());
                }))
            ]
        });

        AppSettings.Change(x => x.Services[ServiceName]["Endpoint"] = SetupUI.AskOpenQuestion("Azure AI Search <endpoint>", config["Endpoint"].ToString()));
        AppSettings.Change(x => x.Services[ServiceName]["UseHybridSearch"] = SetupUI.AskBoolean("Use hybrid search (yes/no)?", (bool)config["UseHybridSearch"]));
        AppSettings.Change(x => x.Services[ServiceName]["UseStickySessions"] = SetupUI.AskBoolean("Use sticky sessions (yes/no)?", (bool)config["UseStickySessions"]));
    }
}
