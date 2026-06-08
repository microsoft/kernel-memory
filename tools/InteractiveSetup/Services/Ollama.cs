// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Globalization;
using Microsoft.KernelMemory.InteractiveSetup.UI;
using Newtonsoft.Json.Linq;

namespace Microsoft.KernelMemory.InteractiveSetup.Services;

internal static class Ollama
{
    public static void Setup(Context ctx, bool force = false)
    {
        if (!ctx.CfgOllamaText.Value && !ctx.CfgOllamaEmbedding.Value && !force) { return; }

        const string ServiceName = "Ollama";

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
                { "ModelName", "phi3:medium-128k" },
                { "MaxTokenTotal", 131072 },
            };

            embeddingModel = new Dictionary<string, object>
            {
                { "ModelName", "nomic-embed-text" },
                { "MaxTokenTotal", 2048 },
            };

            config = new Dictionary<string, object>
            {
                { "Endpoint", "http://localhost:11434" },
                { "TextModel", textModel },
                { "EmbeddingModel", embeddingModel },
            };
            AppSettings.AddService(ServiceName, config);
        }

        AppSettings.Change(x => x.Services[ServiceName] = new Dictionary<string, object>
        {
            { "Endpoint", SetupUI.AskOpenQuestion("Ollama endpoint", config.TryGet("Endpoint")) }
        });

        if (ctx.CfgOllamaText.Value)
        {
            AppSettings.Change(x => x.Services[ServiceName]["TextModel"] = new Dictionary<string, object>
            {
                { "ModelName", SetupUI.AskOpenQuestion("Ollama text model name", textModel.TryGet("ModelName")) },
                { "MaxTokenTotal", SetupUI.AskOpenQuestionInt("Ollama text model max tokens", StrToInt(textModel.TryGet("MaxTokenTotal"))) },
            });
        }

        if (ctx.CfgOllamaEmbedding.Value)
        {
            AppSettings.Change(x => x.Services[ServiceName]["EmbeddingModel"] = new Dictionary<string, object>
            {
                { "ModelName", SetupUI.AskOpenQuestion("Ollama text embedding model name", embeddingModel.TryGet("ModelName")) },
                { "MaxTokenTotal", SetupUI.AskOpenQuestionInt("Ollama text embedding model max tokens", StrToInt(embeddingModel.TryGet("MaxTokenTotal"))) },
            });
        }

        ctx.CfgOllamaText.Value = false;
        ctx.CfgOllamaEmbedding.Value = false;
    }

    private static int StrToInt(string s)
    {
        return int.Parse(s, NumberStyles.Integer, NumberFormatInfo.InvariantInfo);
    }
}
