// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.KernelMemory.InteractiveSetup.UI;

namespace Microsoft.KernelMemory.InteractiveSetup.Services;

internal static class OpenAI
{
    public static void Setup(Context ctx, bool force = false)
    {
        if (!ctx.CfgOpenAI.Value && !force) { return; }

        ctx.CfgOpenAI.Value = false;
        const string ServiceName = "OpenAI";

        if (!AppSettings.GetCurrentConfig().Services.TryGetValue(ServiceName, out var config))
        {
            config = new Dictionary<string, object>
            {
                { "TextModel", "gpt-4o-mini" },
                { "EmbeddingModel", "text-embedding-ada-002" },
                { "APIKey", "" },
                { "OrgId", "" },
                { "MaxRetries", 10 },
            };
        }

        AppSettings.Change(x => x.Services[ServiceName] = new Dictionary<string, object>
        {
            { "TextModel", SetupUI.AskOpenQuestion("OpenAI <text/chat model name>", config.TryGet("TextModel")) },
            { "EmbeddingModel", SetupUI.AskOpenQuestion("OpenAI <embedding model name>", config.TryGet("EmbeddingModel")) },
            { "APIKey", SetupUI.AskPassword("OpenAI <API Key>", config.TryGet("APIKey")) },
            { "OrgId", SetupUI.AskOptionalOpenQuestion("Optional OpenAI <Organization Id>", config.TryGet("OrgId")) },
            { "MaxRetries", 10 },
        });
    }
}
