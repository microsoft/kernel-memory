// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.KernelMemory.InteractiveSetup.UI;

namespace Microsoft.KernelMemory.InteractiveSetup.Services;

internal static class Redis
{
    public static void Setup(Context ctx, bool force = false)
    {
        if (!ctx.CfgRedis.Value && !force) { return; }

        ctx.CfgRedis.Value = false;
        const string ServiceName = "Redis";

        if (!AppSettings.GetCurrentConfig().Services.TryGetValue(ServiceName, out var config))
        {
            config = new Dictionary<string, object>
            {
                ["ConnectionString"] = ""
            };
            AppSettings.AddService(ServiceName, config);
        }

        var connectionString = SetupUI.AskPassword("Redis connection string (e.g. 'localhost:6379,password=..')", config["ConnectionString"].ToString(), optional: true);

        bool AskMoreTags(string additionalMessage)
        {
            string answer = "No";
            SetupUI.AskQuestionWithOptions(new QuestionWithOptions
            {
                Title = $"{additionalMessage}[Redis] Do you want to add a tag (or another tag) to filter memory records?",
                Options =
                [
                    new("Yes", false, () => { answer = "Yes"; }),
                    new("No", true, () => { answer = "No"; })
                ]
            });

            return answer.Equals("Yes", StringComparison.OrdinalIgnoreCase);
        }

        Dictionary<string, string> tagFields = [];

        string additionalMessage = string.Empty;
        while (AskMoreTags(additionalMessage))
        {
            var tagName = SetupUI.AskOpenQuestion("Enter the name of the tag you'd like to filter on, e.g. username", string.Empty);
            if (string.IsNullOrEmpty(tagName))
            {
                additionalMessage = "Unusable tag name entered. ";
                continue;
            }

            var separatorChar = SetupUI.AskOptionalOpenQuestion("How do you want to separate tag values (default is ',')?", ",");
            if (separatorChar.Length > 1)
            {
                additionalMessage = "Unusable separator Char entered. ";
                continue;
            }

            tagFields.Add(tagName, separatorChar);
            additionalMessage = string.Empty;
        }

        AppSettings.Change(x => x.Services[ServiceName] = new Dictionary<string, object>
        {
            { "Tags", tagFields },
            { "ConnectionString", connectionString },
        });
    }
}
