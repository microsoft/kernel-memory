// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.SemanticMemory.Core.Configuration.Dynamic;

public static class VectorDb
{
    public class TypedConfig
    {
        public enum Types
        {
            Unknown = 0,
            AzureCognitiveSearch = 1,
            // Qdrant = 2,
        }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Types Type { get; set; }
    }

    public static object ToVectorDbConfig(this Dictionary<string, object> data)
    {
        var typedItem = data.ToTypedConfig<TypedConfig>(out string json);
        switch (typedItem.Type)
        {
            default:
                throw new ConfigurationException($"Vector db type not supported: {typedItem.Type:G}");

            case TypedConfig.Types.AzureCognitiveSearch:
                return json.JsonDeserializeNotNull<AzureCognitiveSearchConfig>();

            // case VectorStorageConfigTypedConfig.Types.Qdrant:
            //     return json.DeserializeAs<QdrantConfig>();
        }
    }
}
