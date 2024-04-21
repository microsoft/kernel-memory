// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.KernelMemory.InteractiveSetup.UI;

namespace Microsoft.KernelMemory.InteractiveSetup.Services;

internal static class LlamaSharp
{
    public static void Setup(Context ctx, bool force = false)
    {
        if (!ctx.CfgLlamaSharp.Value && !force) { return; }

        ctx.CfgLlamaSharp.Value = false;
        const string ServiceName = "LlamaSharp";

        if (!AppSettings.GetCurrentConfig().Services.TryGetValue(ServiceName, out var config))
        {
            config = new Dictionary<string, object>
            {
                { "ModelPath", "" },
                { "MaxTokenTotal", 4096 },
            };
        }

        AppSettings.Change(x => x.Services[ServiceName] = new Dictionary<string, object>
        {
            { "ModelPath", SetupUI.AskOpenQuestion("Path to model .gguf file", config.TryGet("ModelPath")) },
            { "MaxTokenTotal", SetupUI.AskOpenQuestion("Max tokens supported by the model", config.TryGet("MaxTokenTotal")) },
        });
    }
}
