// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.KernelMemory.InteractiveSetup.UI;

namespace Microsoft.KernelMemory.InteractiveSetup.Services;

internal static class SimpleFileStorage
{
    public static void Setup(Context ctx)
    {
        const string ServiceName = "SimpleFileStorage";

        if (!ctx.CfgSimpleFileStorageVolatile.Value && !ctx.CfgSimpleFileStoragePersistent.Value) { return; }

        var persistent = ctx.CfgSimpleFileStoragePersistent.Value;
        ctx.CfgSimpleFileStorageVolatile.Value = false;
        ctx.CfgSimpleFileStoragePersistent.Value = false;

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
            { "Directory", SetupUI.AskOpenQuestion("Directory where to store files", config["Directory"].ToString()) },
            { "StorageType", persistent ? "Disk" : "Volatile" }
        });
    }
}
