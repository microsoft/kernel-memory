﻿// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.KernelMemory.InteractiveSetup.UI;

namespace Microsoft.KernelMemory.InteractiveSetup.Services;

internal static class SimpleVectorDb
{
    public static void Setup(Context ctx, bool force = false)
    {
        if (!ctx.CfgSimpleVectorDb.Value && !force) { return; }

        ctx.CfgSimpleVectorDb.Value = false;
        const string ServiceName = "SimpleVectorDb";

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
            { "Directory", SetupUI.AskOpenQuestion("Directory where to store vectors", config["Directory"].ToString()) }
        });
    }
}
