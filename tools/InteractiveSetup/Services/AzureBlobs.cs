// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.KernelMemory.InteractiveSetup.UI;

namespace Microsoft.KernelMemory.InteractiveSetup.Services;

internal static class AzureBlobs
{
    public static void Setup(Context ctx)
    {
        if (!ctx.CfgAzureBlobs.Value) { return; }

        ctx.CfgAzureBlobs.Value = false;
        const string ServiceName = "AzureBlobs";

        if (!AppSettings.GetCurrentConfig().Services.TryGetValue(ServiceName, out var config))
        {
            config = new Dictionary<string, object>
            {
                { "Auth", "ConnectionString" },
                { "Account", "" },
                { "Container", "smemory" },
                { "ConnectionString", "" },
            };
        }

        AppSettings.Change(x => x.Services[ServiceName] = new Dictionary<string, object>
        {
            { "Auth", "ConnectionString" },
            { "Container", SetupUI.AskOpenQuestion("Azure Blobs <container name>", config["Container"].ToString()) },
            { "Account", SetupUI.AskOpenQuestion("Azure Blobs <account name>", config["Account"].ToString()) },
            { "ConnectionString", SetupUI.AskPassword("Azure Blobs <connection string>", config["ConnectionString"].ToString()) },
        });
    }
}
