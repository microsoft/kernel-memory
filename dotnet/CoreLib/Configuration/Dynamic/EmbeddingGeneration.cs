// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.SemanticMemory.Core.Configuration.Dynamic;

public static class EmbeddingGeneration
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

    public static object ToEmbeddingGenerationConfig(this Dictionary<string, object> data)
    {
        var typedItem = data.ToTypedConfig<TypedConfig>(out string json);
        switch (typedItem.Type)
        {
            default:
                throw new ConfigurationException($"Embedding generation type not supported: {typedItem.Type:G}");

            case TypedConfig.Types.AzureOpenAI:
                return json.JsonDeserializeNotNull<AzureOpenAIConfig>();

            case TypedConfig.Types.OpenAI:
                return json.JsonDeserializeNotNull<OpenAIConfig>();
        }
    }
}
