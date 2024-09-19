// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Globalization;
using Microsoft.KernelMemory.InteractiveSetup.UI;

namespace Microsoft.KernelMemory.InteractiveSetup.Services;

internal static class Ollama
{
    public static void Setup(Context ctx, bool force = false)
    {
        if (!ctx.CfgOllama.Value && !force) { return; }

        ctx.CfgOllama.Value = false;
        const string ServiceName = "Ollama";

        Dictionary<string, object> textModel = new();
        Dictionary<string, object> embeddingModel = new();

        if (!AppSettings.GetCurrentConfig().Services.TryGetValue(ServiceName, out var config))
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
        }

        AppSettings.Change(x => x.Services[ServiceName] = new Dictionary<string, object>
        {
            { "Endpoint", SetupUI.AskOpenQuestion("Ollama endpoint", config.TryGet("Endpoint")) }
        });

        AppSettings.Change(x => x.Services[ServiceName]["TextModel"] = new Dictionary<string, object>
        {
            { "ModelName", SetupUI.AskOpenQuestion("Ollama text model name", textModel.TryGet("ModelName")) },
            { "MaxTokenTotal", SetupUI.AskOpenQuestionInt("Ollama text model max tokens", StrToInt(textModel.TryGet("MaxTokenTotal"))) },
        });

        AppSettings.Change(x => x.Services[ServiceName]["EmbeddingModel"] = new Dictionary<string, object>
        {
            { "ModelName", SetupUI.AskOpenQuestion("Ollama text embedding model name", embeddingModel.TryGet("ModelName")) },
            { "MaxTokenTotal", SetupUI.AskOpenQuestionInt("Ollama text embedding model max tokens", StrToInt(embeddingModel.TryGet("MaxTokenTotal"))) },
        });
    }

    private static int StrToInt(string s)
    {
        return int.Parse(s, NumberStyles.Integer, NumberFormatInfo.InvariantInfo);
    }
}
