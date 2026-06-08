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
                { "Account", "" },
                { "Container", "kmemory" },
                { "Auth", "ConnectionString" },
                { "ConnectionString", "" },
            };
            AppSettings.AddService(ServiceName, config);
        }

        SetupUI.AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = $"[{ServiceName}] Which type of authentication do you want to use?",
            Options =
            [
                new("Azure Identity (Entra)", config["Auth"].ToString() == "AzureIdentity", () => AppSettings.Change(x =>
                {
                    x.Services[ServiceName]["Auth"] = "AzureIdentity";
                    x.Services[ServiceName].Remove("ConnectionString");
                })),
                new("Connection String", config["Auth"].ToString() != "AzureIdentity", () => AppSettings.Change(x =>
                {
                    x.Services[ServiceName]["Auth"] = "ConnectionString";
                    x.Services[ServiceName]["ConnectionString"] = SetupUI.AskPassword("Azure Blobs <connection string>", config["ConnectionString"].ToString());
                }))
            ]
        });

        AppSettings.Change(x => x.Services[ServiceName]["Account"] = SetupUI.AskOpenQuestion("Azure Blobs <account name>", config["Account"].ToString()));
        AppSettings.Change(x => x.Services[ServiceName]["Container"] = SetupUI.AskOpenQuestion("Azure Blobs <container name>", config["Container"].ToString()));
    }
}
