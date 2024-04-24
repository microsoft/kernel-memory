// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.KernelMemory.InteractiveSetup.UI;

namespace Microsoft.KernelMemory.InteractiveSetup.Services;

internal static class Qdrant
{
    public static void Setup(Context ctx, bool force = false)
    {
        if (!ctx.CfgQdrant.Value && !force) { return; }

        ctx.CfgQdrant.Value = false;
        const string ServiceName = "Qdrant";

        if (!AppSettings.GetCurrentConfig().Services.TryGetValue(ServiceName, out var config))
        {
            config = new Dictionary<string, object>
            {
                { "Endpoint", "http://127.0.0.1:6333" },
                { "APIKey", "" },
            };
        }

        AppSettings.Change(x => x.Services[ServiceName] = new Dictionary<string, object>
        {
            { "Endpoint", SetupUI.AskOpenQuestion("Qdrant <endpoint>", config["Endpoint"].ToString()) },
            { "APIKey", SetupUI.AskPassword("Qdrant <API Key> (for cloud only)", config["APIKey"].ToString(), optional: true) },
        });
    }
}
