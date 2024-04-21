// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.KernelMemory.InteractiveSetup.UI;

namespace Microsoft.KernelMemory.InteractiveSetup.Services;

internal static class AzureQueue
{
    public static void Setup(Context ctx)
    {
        if (!ctx.CfgAzureQueue.Value) { return; }

        ctx.CfgAzureQueue.Value = false;
        const string ServiceName = "AzureQueues";

        if (!AppSettings.GetCurrentConfig().Services.TryGetValue(ServiceName, out var config))
        {
            config = new Dictionary<string, object>
            {
                { "Auth", "ConnectionString" },
                { "Account", "" },
                { "ConnectionString", "" },
            };
        }

        AppSettings.Change(x => x.Services[ServiceName] = new Dictionary<string, object>
        {
            { "Auth", "ConnectionString" },
            { "Account", SetupUI.AskOpenQuestion("Azure Queue <account name>", config["Account"].ToString()) },
            { "ConnectionString", SetupUI.AskPassword("Azure Queue <connection string>", config["ConnectionString"].ToString()) },
        });
    }
}
