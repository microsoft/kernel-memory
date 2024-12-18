// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.KernelMemory.InteractiveSetup.UI;

namespace Microsoft.KernelMemory.InteractiveSetup.Services;

internal static class RabbitMQ
{
    public static void Setup(Context ctx, bool force = false)
    {
        if (!ctx.CfgRabbitMq.Value && !force) { return; }

        ctx.CfgRabbitMq.Value = false;
        const string ServiceName = "RabbitMQ";

        if (!AppSettings.GetCurrentConfig().Services.TryGetValue(ServiceName, out var config))
        {
            config = new Dictionary<string, object>
            {
                { "Host", "127.0.0.1" },
                { "Port", "5672" },
                { "Username", "user" },
                { "Password", "" },
                { "VirtualHost", "/" },
                { "SslEnabled", false },
            };
            AppSettings.AddService(ServiceName, config);
        }

        AppSettings.Change(x => x.Services[ServiceName] = new Dictionary<string, object>
        {
            { "Host", SetupUI.AskOpenQuestion("RabbitMQ <host>", config["Host"].ToString()) },
            { "Port", SetupUI.AskOpenQuestion("RabbitMQ <TCP port>", config["Port"].ToString()) },
            { "Username", SetupUI.AskOpenQuestion("RabbitMQ <username>", config["Username"].ToString()) },
            { "Password", SetupUI.AskPassword("RabbitMQ <password>", config["Password"].ToString()) },
            { "VirtualHost", SetupUI.AskOpenQuestion("RabbitMQ <virtualhost>", config["VirtualHost"].ToString()) },
            { "SslEnabled", SetupUI.AskBoolean("RabbitMQ SSL enabled (yes/no)?", (bool)config["SslEnabled"]) },
        });
    }
}
