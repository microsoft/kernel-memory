// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.KernelMemory.InteractiveSetup.UI;

namespace Microsoft.KernelMemory.InteractiveSetup.Services;

internal static class AzureAIDocIntel
{
    public static void Setup(Context ctx)
    {
        if (!ctx.CfgAzureAIDocIntel.Value) { return; }

        ctx.CfgAzureAIDocIntel.Value = false;
        const string ServiceName = "AzureAIDocIntel";

        if (!AppSettings.GetCurrentConfig().Services.TryGetValue(ServiceName, out var config))
        {
            config = new Dictionary<string, object>
            {
                { "Endpoint", "" },
                { "Auth", "ApiKey" },
                { "APIKey", "" },
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
                    x.Services[ServiceName]["APIKey"] = SetupUI.AskPassword("Azure AI <API Key>", config["APIKey"].ToString());
                }))
            ]
        });

        AppSettings.Change(x => x.Services[ServiceName]["Endpoint"] = SetupUI.AskOpenQuestion("Azure AI <endpoint>", config["Endpoint"].ToString()));
    }
}
