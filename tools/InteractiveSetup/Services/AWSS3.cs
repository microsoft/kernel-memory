// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.KernelMemory.InteractiveSetup.UI;

namespace Microsoft.KernelMemory.InteractiveSetup.Services;

internal static class AWSS3
{
    public static void Setup(Context ctx)
    {
        if (!ctx.CfgAWSS3.Value) { return; }

        ctx.CfgAWSS3.Value = false;
        const string ServiceName = "AWSS3";

        if (!AppSettings.GetCurrentConfig().Services.TryGetValue(ServiceName, out var config))
        {
            config = new Dictionary<string, object>
            {
                { "Auth", "AccessKey" },
                { "AccessKey", "" },
                { "SecretAccessKey", "" },
                { "BucketName", "" },
                { "Endpoint", "https://s3.amazonaws.com" },
            };
        }

        // Required to avoid exceptions. "Endpoint" is optional and not defined in appsettings.json
        config.TryAdd("Endpoint", "https://s3.amazonaws.com");

        AppSettings.Change(x => x.Services[ServiceName] = new Dictionary<string, object>
        {
            { "Auth", "AccessKey" },
            { "AccessKey", SetupUI.AskOpenQuestion("AWS S3 <access key>", config["AccessKey"].ToString()) },
            { "SecretAccessKey", SetupUI.AskPassword("AWS S3 <secret access key>", config["SecretAccessKey"].ToString()) },
            { "BucketName", SetupUI.AskOpenQuestion("AWS S3 <bucket name>", config["BucketName"].ToString()) },
            { "Endpoint", SetupUI.AskOpenQuestion("AWS S3 <endpoint>", config["Endpoint"].ToString()) },
        });
    }
}
