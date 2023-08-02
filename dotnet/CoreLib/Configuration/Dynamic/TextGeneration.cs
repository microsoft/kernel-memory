// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.SemanticMemory.Core.Configuration.Dynamic;

public static class TextGeneration
{
    public class TypedConfig
    {
        public enum Types
        {
            Unknown = 0,
            AzureOpenAI = 1,
            OpenAI = 2,
        }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Types Type { get; set; }
    }

    public static object ToTextGenerationConfig(this Dictionary<string, object> data)
    {
        TypedConfig typedItem;
        string json;
        try
        {
            typedItem = data.ToTypedConfig<TypedConfig>(out json);
        }
        catch (Exception e)
        {
            throw new ConfigurationException($"Unable to load text generation settings: {e.Message}", e);
        }

        switch (typedItem.Type)
        {
            default:
                throw new ConfigurationException($"Text generation type not supported: {typedItem.Type:G}");

            case TypedConfig.Types.AzureOpenAI:
                return json.JsonDeserializeNotNull<AzureOpenAIConfig>();

            case TypedConfig.Types.OpenAI:
                return json.JsonDeserializeNotNull<OpenAIConfig>();
        }
    }
}
