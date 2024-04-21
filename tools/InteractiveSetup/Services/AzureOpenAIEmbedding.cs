// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.KernelMemory.InteractiveSetup.UI;

namespace Microsoft.KernelMemory.InteractiveSetup.Services;

internal static class AzureOpenAIEmbedding
{
    public static void Setup(Context ctx, bool force = false)
    {
        if (!ctx.CfgAzureOpenAIEmbedding.Value && !force) { return; }

        ctx.CfgAzureOpenAIEmbedding.Value = false;
        const string ServiceName = "AzureOpenAIEmbedding";

        if (!AppSettings.GetCurrentConfig().Services.TryGetValue(ServiceName, out var config))
        {
            config = new Dictionary<string, object>
            {
                { "Auth", "ApiKey" },
                { "Endpoint", "" },
                { "Deployment", "" },
                { "APIKey", "" },
            };
        }

        AppSettings.Change(x => x.Services[ServiceName] = new Dictionary<string, object>
        {
            { "APIType", "EmbeddingGeneration" },
            { "Auth", "ApiKey" },
            { "Endpoint", SetupUI.AskOpenQuestion("Azure OpenAI <endpoint>", config["Endpoint"].ToString()) },
            { "Deployment", SetupUI.AskOpenQuestion("Azure OpenAI <embedding model deployment name>", config["Deployment"].ToString()) },
            { "APIKey", SetupUI.AskPassword("Azure OpenAI <API Key>", config["APIKey"].ToString()) },
        });
    }
}
