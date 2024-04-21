// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.KernelMemory.InteractiveSetup.UI;

namespace Microsoft.KernelMemory.InteractiveSetup.Services;

internal static class SimpleFileStorage
{
    public static void Setup(Context ctx)
    {
        if (!ctx.CfgSimpleFileStorage.Value) { return; }

        ctx.CfgSimpleFileStorage.Value = false;
        const string ServiceName = "SimpleFileStorage";

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
            { "Directory", SetupUI.AskOpenQuestion("Directory where to store files", config["Directory"].ToString()) }
        });
    }
}
