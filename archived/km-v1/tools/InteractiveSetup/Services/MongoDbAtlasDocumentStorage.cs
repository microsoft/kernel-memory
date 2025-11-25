// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.KernelMemory.InteractiveSetup.UI;

namespace Microsoft.KernelMemory.InteractiveSetup.Services;

internal static class MongoDbAtlasDocumentStorage
{
    public static void Setup(Context ctx, bool force = false)
    {
        if (!ctx.CfgMongoDbAtlasDocumentStorage.Value && !force) { return; }

        ctx.CfgMongoDbAtlasDocumentStorage.Value = false;
        const string ServiceName = "MongoDbAtlas";

        if (!AppSettings.GetCurrentConfig().Services.TryGetValue(ServiceName, out var config))
        {
            config = new Dictionary<string, object>
            {
                { "ConnectionString", "" }
            };
            AppSettings.AddService(ServiceName, config);
        }

        AppSettings.Change(x => x.Services[ServiceName] = new Dictionary<string, object>
        {
            {
                "ConnectionString",
                SetupUI.AskPassword("MongoDB Atlas connection string (e.g. 'mongodb://usr:pwd@host:port/?...')", config["ConnectionString"].ToString(), optional: false)
            },
        });
    }
}
