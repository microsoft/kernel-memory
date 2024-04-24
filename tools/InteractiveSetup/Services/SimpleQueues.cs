// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.KernelMemory.InteractiveSetup.UI;

namespace Microsoft.KernelMemory.InteractiveSetup.Services;

internal static class SimpleQueues
{
    public static void Setup(Context ctx)

    {
        if (!ctx.CfgSimpleQueues.Value) { return; }

        ctx.CfgSimpleQueues.Value = false;
        const string ServiceName = "SimpleQueues";

        if (!AppSettings.GetCurrentConfig().Services.TryGetValue(ServiceName, out var config))
        {
            config = new Dictionary<string, object>
            {
                { "Directory", "" },
                { "StorageType", "Volatile" }
            };
        }

        AppSettings.Change(x => x.Services[ServiceName] = new Dictionary<string, object>
        {
            { "Directory", SetupUI.AskOpenQuestion("Directory where to store queue messages", config["Directory"].ToString()) }
        });
    }
}
