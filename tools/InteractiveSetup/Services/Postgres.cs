// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.KernelMemory.InteractiveSetup.UI;

namespace Microsoft.KernelMemory.InteractiveSetup.Services;

internal static class Postgres
{
    public static void Setup(Context ctx, bool force = false)
    {
        if (!ctx.CfgPostgres.Value && !force) { return; }

        ctx.CfgPostgres.Value = false;
        const string ServiceName = "Postgres";

        if (!AppSettings.GetCurrentConfig().Services.TryGetValue(ServiceName, out var config))
        {
            config = new Dictionary<string, object>
            {
                { "ConnectionString", "" },
            };
        }

        AppSettings.Change(x => x.Services[ServiceName] = new Dictionary<string, object>
        {
            {
                "ConnectionString",
                SetupUI.AskPassword("Postgres connection string (e.g. 'Host=..;Port=5432;Username=..;Password=..')", config["ConnectionString"].ToString(), optional: false)
            },
        });
    }
}
