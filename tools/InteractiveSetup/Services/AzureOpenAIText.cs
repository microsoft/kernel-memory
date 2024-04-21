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
                { "Auth", "ApiKey" },
                { "Endpoint", "" },
                { "Deployment", "" },
                { "APIKey", "" },
                { "MaxRetries", 10 },
            };
        }

        AppSettings.Change(x => x.Services[ServiceName] = new Dictionary<string, object>
        {
            { "APIType", "ChatCompletion" },
            { "Auth", "ApiKey" },
            { "Endpoint", SetupUI.AskOpenQuestion("Azure OpenAI <endpoint>", config["Endpoint"].ToString()) },
            { "Deployment", SetupUI.AskOpenQuestion("Azure OpenAI <text/chat completion deployment name>", config["Deployment"].ToString()) },
            { "APIKey", SetupUI.AskPassword("Azure OpenAI <API Key>", config["APIKey"].ToString()) },
            { "MaxRetries", 10 },
        });
    }
}
