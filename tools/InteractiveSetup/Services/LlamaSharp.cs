// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.KernelMemory.InteractiveSetup.UI;
using Newtonsoft.Json.Linq;

namespace Microsoft.KernelMemory.InteractiveSetup.Services;

internal static class LlamaSharp
{
    public static void Setup(Context ctx, bool force = false)
    {
        if (!ctx.CfgLlamaSharpText.Value && !ctx.CfgLlamaSharpEmbedding.Value && !force) { return; }

        const string ServiceName = "LlamaSharp";

        Dictionary<string, object> textModel = [];
        Dictionary<string, object> embeddingModel = [];

        if (AppSettings.GetCurrentConfig().Services.TryGetValue(ServiceName, out var config))
        {
            if (config.TryGetValue("TextModel", out object? tm) && tm is JObject jtm)
            {
                textModel = jtm.ToObject<Dictionary<string, object>>() ?? [];
            }

            if (config.TryGetValue("EmbeddingModel", out object? em) && em is JObject jem)
            {
                embeddingModel = jem.ToObject<Dictionary<string, object>>() ?? [];
            }
        }
        else
        {
            textModel = new Dictionary<string, object>
            {
                { "ModelPath", "" },
                { "MaxTokenTotal", 4096 },
            };

            embeddingModel = new Dictionary<string, object>
            {
                { "ModelPath", "" },
                { "MaxTokenTotal", 4096 },
            };

            config = new Dictionary<string, object>
            {
                { "TextModel", textModel },
                { "EmbeddingModel", embeddingModel }
            };
            AppSettings.AddService(ServiceName, config);
        }

        if (ctx.CfgLlamaSharpText.Value)
        {
            AppSettings.Change(x => x.Services[ServiceName] = new Dictionary<string, object>
            {
                {
                    "TextModel", new Dictionary<string, object>
                    {
                        { "ModelPath", SetupUI.AskOpenQuestion("Path to text model .gguf file", textModel.TryGet("ModelPath")) },
                        { "MaxTokenTotal", SetupUI.AskOpenQuestion("Max tokens supported by the text model", textModel.TryGet("MaxTokenTotal")) },
                    }
                }
            });
        }

        if (ctx.CfgLlamaSharpEmbedding.Value)
        {
            AppSettings.Change(x => x.Services[ServiceName] = new Dictionary<string, object>
            {
                {
                    "EmbeddingModel", new Dictionary<string, object>
                    {
                        { "ModelPath", SetupUI.AskOpenQuestion("Path to embedding model .gguf file", embeddingModel.TryGet("ModelPath")) },
                        { "MaxTokenTotal", SetupUI.AskOpenQuestion("Max tokens supported by the embedding model", embeddingModel.TryGet("MaxTokenTotal")) },
                    }
                }
            });
        }

        ctx.CfgLlamaSharpText.Value = false;
        ctx.CfgLlamaSharpEmbedding.Value = false;
    }
}
