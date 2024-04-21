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
                { "Auth", "ApiKey" },
                { "Endpoint", "" },
                { "APIKey", "" },
            };
        }

        AppSettings.Change(x => x.Services[ServiceName] = new Dictionary<string, object>
        {
            { "Auth", "ApiKey" },
            { "Endpoint", SetupUI.AskOpenQuestion("Azure AI <endpoint>", config["Endpoint"].ToString()) },
            { "APIKey", SetupUI.AskPassword("Azure AI <API Key>", config["APIKey"].ToString()) },
        });
    }
}
