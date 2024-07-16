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
                { "Account", "" },
                { "Auth", "ConnectionString" },
                { "ConnectionString", "" },
            };
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
                    x.Services[ServiceName]["ConnectionString"] = SetupUI.AskPassword("Azure Queue <connection string>", config["ConnectionString"].ToString());
                }))
            ]
        });

        AppSettings.Change(x => x.Services[ServiceName]["Account"] = SetupUI.AskOpenQuestion("Azure Queue <account name>", config["Account"].ToString()));
    }
}
