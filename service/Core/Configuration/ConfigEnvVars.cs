// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.KernelMemory.Configuration;

public static class ConfigEnvVars
{
    /// <summary>
    /// Generate env vars to override settings in appsettings.json
    /// </summary>
    /// <param name="source">Configuration settings</param>
    /// <param name="parents">Namespace within the appsettings.json file</param>
    /// <returns>Environment variables</returns>
    public static Dictionary<string, string> GenerateEnvVarsFromObject(
        object? source, params string[] parents)
    {
        if (source == null) { return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); }

        var prefix = GetPrefix(parents);
        return GenerateEnvVars(source, prefix);
    }

    /// <summary>
    /// Generate env vars to override settings in appsettings.json,
    /// ignoring defaults found in the type.
    /// Note: defaults in appsettings.json might differ.
    /// </summary>
    /// <param name="source">Configuration settings</param>
    /// <param name="parents">Namespace within the appsettings.json file</param>
    /// <returns>Environment variables</returns>
    public static Dictionary<string, string> GenerateEnvVarsFromObjectNoDefaults(
        object? source, params string[] parents)
    {
        if (source == null) { return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); }

        var variables = GenerateEnvVarsFromObject(source, parents);
        var prefix = GetPrefix(parents);

        var defaults = GenerateEnvVars(CreateInstanceOfSameType(source), prefix);
        foreach (var pair in defaults)
        {
            if (variables.TryGetValue(pair.Key, out string? value) && value == pair.Value)
            {
                variables.Remove(pair.Key);
            }
        }

        return variables;
    }

    private static string GetPrefix(params string[] parents)
    {
        return parents.Length > 0 ? string.Join("__", parents) + "__" : string.Empty;
    }

    private static Dictionary<string, string> GenerateEnvVars(object source, string prefix)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var objProperties = source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in objProperties)
        {
            var fullKey = $"{prefix}{property.Name}";
            var value = property.GetValue(source);

            if (value is IDictionary dictionary)
            {
                foreach (var key in dictionary.Keys)
                {
                    if (dictionary[key] != null)
                    {
                        var dictKey = $"{fullKey}__{key}";
                        result[dictKey] = dictionary[key]?.ToString() ?? string.Empty;
                    }
                }
            }
            else if (value is IEnumerable enumerable and not string)
            {
                int index = 0;
                foreach (var item in enumerable)
                {
                    var arrayKey = $"{fullKey}__{index}";
                    result[arrayKey] = item?.ToString() ?? string.Empty;
                    index++;
                }
            }
            else if (value != null)
            {
                result[fullKey] = value.ToString() ?? string.Empty;
            }
        }

        return result;
    }

    private static object CreateInstanceOfSameType(object source)
    {
        var type = source.GetType();
        var result = Activator.CreateInstance(type);

        return result ?? throw new InvalidOperationException($"Unable to create instance of type {type.FullName}");
    }
}
