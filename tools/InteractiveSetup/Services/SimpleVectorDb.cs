// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.KernelMemory.InteractiveSetup.UI;

namespace Microsoft.KernelMemory.InteractiveSetup.Services;

internal static class SimpleVectorDb
{
    public static void Setup(Context ctx, bool force = false)
    {
        const string ServiceName = "SimpleVectorDb";

        if (!ctx.CfgSimpleVectorDbVolatile.Value && !ctx.CfgSimpleVectorDbPersistent.Value) { return; }

        var persistent = ctx.CfgSimpleVectorDbPersistent.Value;
        ctx.CfgSimpleVectorDbVolatile.Value = false;
        ctx.CfgSimpleVectorDbPersistent.Value = false;

        if (!AppSettings.GetCurrentConfig().Services.TryGetValue(ServiceName, out var config))
        {
            config = new Dictionary<string, object>
            {
                { "Directory", "" },
                { "StorageType", "Disk" }
            };
            AppSettings.AddService(ServiceName, config);
        }

        AppSettings.Change(x => x.Services[ServiceName] = new Dictionary<string, object>
        {
            { "Directory", SetupUI.AskOpenQuestion("Directory where to store vectors", config["Directory"].ToString()) },
            { "StorageType", persistent ? "Disk" : "Volatile" }
        });
    }
}
