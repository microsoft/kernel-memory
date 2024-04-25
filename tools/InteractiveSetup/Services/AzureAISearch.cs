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
                { "Auth", "ApiKey" },
                { "Endpoint", "" },
                { "APIKey", "" },
                { "UseHybridSearch", false },
            };
        }

        AppSettings.Change(x => x.Services[ServiceName] = new Dictionary<string, object>
        {
            { "Auth", "ApiKey" },
            { "Endpoint", SetupUI.AskOpenQuestion("Azure AI Search <endpoint>", config["Endpoint"].ToString()) },
            { "APIKey", SetupUI.AskPassword("Azure AI Search <API Key>", config["APIKey"].ToString()) },
            { "UseHybridSearch", SetupUI.AskBoolean("Use hybrid search (yes/no)?", (bool)config["UseHybridSearch"]) },
        });
    }
}
