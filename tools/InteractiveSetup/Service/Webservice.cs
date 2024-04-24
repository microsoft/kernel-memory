// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.KernelMemory.InteractiveSetup.UI;

namespace Microsoft.KernelMemory.InteractiveSetup.Service;

internal static class Webservice
{
    public static void Setup(Context ctx, bool force = false)
    {
        if (!ctx.CfgWebService.Value && !force) { return; }

        var config = AppSettings.GetCurrentConfig();

        SetupUI.AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "Protect the web service with API Keys?",
            Description = "If the web service runs on a public network it should protected requiring clients to pass one of two secret API keys on each request. The API Key is passed using the `Authorization` HTTP header.",
            Options = new List<Answer>
            {
                new("Yes", config.ServiceAuthorization.Enabled, () =>
                {
                    AppSettings.Change(x =>
                    {
                        x.ServiceAuthorization.Enabled = true;
                        x.ServiceAuthorization.HttpHeaderName = "Authorization";
                        x.ServiceAuthorization.AccessKey1 = SetupUI.AskPassword("API Key 1 (min 32 chars, alphanumeric ('- . _' allowed))", x.ServiceAuthorization.AccessKey1);
                        x.ServiceAuthorization.AccessKey2 = SetupUI.AskPassword("API Key 2 (min 32 chars, alphanumeric ('- . _' allowed))", x.ServiceAuthorization.AccessKey2);
                    });
                }),
                new("No", !config.ServiceAuthorization.Enabled, () =>
                {
                    AppSettings.Change(x =>
                    {
                        x.ServiceAuthorization.Enabled = false;
                        x.ServiceAuthorization.HttpHeaderName = "Authorization";
                        x.ServiceAuthorization.AccessKey1 = "";
                        x.ServiceAuthorization.AccessKey2 = "";
                    });
                }),
                new("-exit-", false, SetupUI.Exit),
            }
        });

        SetupUI.AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "Enable OpenAPI swagger doc at /swagger/index.html?",
            Options = new List<Answer>
            {
                new("Yes", config.Service.OpenApiEnabled, () => { AppSettings.Change(x => { x.Service.OpenApiEnabled = true; }); }),
                new("No", !config.Service.OpenApiEnabled, () => { AppSettings.Change(x => { x.Service.OpenApiEnabled = false; }); }),
                new("-exit-", false, SetupUI.Exit),
            }
        });
    }
}
