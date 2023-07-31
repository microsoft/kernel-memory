// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Text.Json;

namespace Microsoft.SemanticMemory.Core.Configuration.Dynamic;

/// <summary>
/// For generic configurations that might have any number of properties, use the "Type"
/// property to find the correct class to deserialize to.
/// </summary>
public static class DynamicConfig
{
    public static T ToTypedConfig<T>(this Dictionary<string, object> data, out string json)
    {
        // The list of properties cannot be empty
        if (data.Count == 0)
        {
            throw new ConfigurationException("The configuration is empty");
        }

        // The list of properties must contain "Type" to detect the class to use
        if (!data.ContainsKey("Type") && !data.ContainsKey("type") && !data.ContainsKey("TYPE"))
        {
            throw new ConfigurationException("The configuration must contain a 'Type' property");
        }

        json = JsonSerializer.Serialize(data);

        // The JSON representation cannot be empty, otherwise the code later
        // will fail to deserialize data to the correct class
        if (string.IsNullOrEmpty(json))
        {
            throw new ConfigurationException("The configuration serialized to an empty JSON");
        }

        // Deserialize to a simple class that has just the "Type" property, making sure the deserialization succeeds
        var result = JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (result == null)
        {
            throw new ConfigurationException("Unable to deserialize configuration, the result is NULL");
        }

        return result;
    }

    // Deserialize the JSON string to the requested configuration class, checking that the result is not null
    public static T JsonDeserializeNotNull<T>(this string json)
    {
        var result = JsonSerializer.Deserialize<T>(json);
        if (result == null)
        {
            throw new ConfigurationException("Unable to deserialize configuration, the result is NULL");
        }

        return result;
    }
}
