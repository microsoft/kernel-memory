// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.KernelMemory.InteractiveSetup.UI;

namespace Microsoft.KernelMemory.InteractiveSetup.Services;

internal static class AzureOpenAIText
{
    public static void Setup(Context ctx, bool force = false)
    {
        if (!ctx.CfgAzureOpenAIText.Value && !force) { return; }

        ctx.CfgAzureOpenAIText.Value = false;
        const string ServiceName = "AzureOpenAIText";

        if (!AppSettings.GetCurrentConfig().Services.TryGetValue(ServiceName, out var config))
        {
            config = new Dictionary<string, object>
            {
                { "APIType", "ChatCompletion" },
                { "Endpoint", "" },
                { "Deployment", "" },
                { "Auth", "ApiKey" },
                { "APIKey", "" },
            };
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
                    x.Services[ServiceName]["APIKey"] = SetupUI.AskPassword("Azure OpenAI <API Key>", config["APIKey"].ToString());
                }))
            ]
        });

        AppSettings.Change(x => x.Services[ServiceName]["APIType"] = "ChatCompletion");
        AppSettings.Change(x => x.Services[ServiceName]["Endpoint"] = SetupUI.AskOpenQuestion("Azure OpenAI <endpoint>", config["Endpoint"].ToString()));
        AppSettings.Change(x => x.Services[ServiceName]["Deployment"] = SetupUI.AskOpenQuestion("Azure OpenAI <text/chat model deployment name>", config["Deployment"].ToString()));
    }
}
