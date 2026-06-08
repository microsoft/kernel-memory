// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.KernelMemory.InteractiveSetup.UI;

namespace Microsoft.KernelMemory.InteractiveSetup.Services;

internal static class SimpleQueues
{
    public static void Setup(Context ctx)
    {
        const string ServiceName = "SimpleQueues";

        if (!ctx.CfgSimpleQueuesVolatile.Value && !ctx.CfgSimpleQueuesPersistent.Value) { return; }

        var persistent = ctx.CfgSimpleQueuesPersistent.Value;
        ctx.CfgSimpleQueuesVolatile.Value = false;
        ctx.CfgSimpleQueuesPersistent.Value = false;

        if (!AppSettings.GetCurrentConfig().Services.TryGetValue(ServiceName, out var config))
        {
            config = new Dictionary<string, object>
            {
                { "Directory", "" }
            };
            AppSettings.AddService(ServiceName, config);
        }

        AppSettings.Change(x => x.Services[ServiceName] = new Dictionary<string, object>
        {
            { "Directory", SetupUI.AskOpenQuestion("Directory where to store queue messages", config["Directory"].ToString()) },
            { "StorageType", persistent ? "Disk" : "Volatile" }
        });
    }
}
