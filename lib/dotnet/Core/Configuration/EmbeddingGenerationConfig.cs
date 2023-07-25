// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.SemanticKernel.SemanticMemory.Core.Configuration;

/// <summary>
/// Configuration settings for the embedding generators
/// </summary>
public class EmbeddingGenerationConfig
{
    /// <summary>
    /// List of active generators, out of the full list.
    /// <see cref="GeneratorsConfig"/>  might contain settings for several generators, but normally only one is in use.
    /// </summary>
    public List<string> ActiveGenerators { get; set; } = new();

    /// <summary>
    /// Available embedding generators, with settings.
    /// Settings here are stored as string values, and parsed to actual types by <see cref="GetActiveGeneratorsTypedConfig"/>
    /// </summary>
    public Dictionary<string, Dictionary<string, string>> GeneratorsConfig { get; set; } = new();

    /// <summary>
    /// Known embedding generator types.
    /// TODO: add SentenceTransformers
    /// </summary>
    public enum GeneratorTypes
    {
        Unknown = 0,
        AzureOpenAI = 1,
        OpenAI = 2,
    }

    /// <summary>
    /// Azure OpenAI embedding generator settings.
    /// </summary>
    public class AzureOpenAI
    {
        public GeneratorTypes Type { get; } = GeneratorTypes.AzureOpenAI;
        public string APIKey { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public string Deployment { get; set; } = string.Empty;
    }

    /// <summary>
    /// OpenAI embedding generator settings.
    /// </summary>
    public class OpenAI
    {
        public GeneratorTypes Type { get; } = GeneratorTypes.OpenAI;
        public string APIKey { get; set; } = string.Empty;
        public string OrgId { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
    }

    /// <summary>
    /// Cast settings from <see cref="GeneratorsConfig"/> to actual typed values.
    /// </summary>
    /// <param name="log">Optional logger</param>
    /// <returns>Strongly typed view of active generators</returns>
    public Dictionary<string, object> GetActiveGeneratorsTypedConfig(ILogger? log = null)
    {
        log ??= NullLogger<EmbeddingGenerationConfig>.Instance;

        Dictionary<string, object> result = new();
        foreach (string name in this.ActiveGenerators)
        {
            result[name] = this.GetGeneratorConfig(name);
            switch (result[name])
            {
                case AzureOpenAI x:
                    log.LogDebug("Using Azure OpenAI embeddings, deployment: {0}", x.Deployment);
                    break;

                case OpenAI x:
                    log.LogDebug("Using OpenAI embeddings, model: {0}", x.Model);
                    break;
            }
        }

        return result;
    }

    private object GetGeneratorConfig(string name)
    {
        string type = this.GeneratorsConfig[name]["Type"];

        if (string.Equals(type, GeneratorTypes.AzureOpenAI.ToString("G"), StringComparison.OrdinalIgnoreCase))
        {
            return new AzureOpenAI
            {
                APIKey = this.GetGeneratorSetting(name, "APIKey"),
                Endpoint = this.GetGeneratorSetting(name, "Endpoint"),
                Deployment = this.GetGeneratorSetting(name, "Deployment"),
            };
        }

        if (string.Equals(type, GeneratorTypes.OpenAI.ToString("G"), StringComparison.OrdinalIgnoreCase))
        {
            return new OpenAI
            {
                APIKey = this.GetGeneratorSetting(name, "APIKey"),
                OrgId = this.GetGeneratorSetting(name, "OrgId", true),
                Model = this.GetGeneratorSetting(name, "Model"),
            };
        }

        throw new ConfigurationException($"Embedding generator type '{this.GeneratorsConfig[name]["Type"]}' not supported");
    }

    private string GetGeneratorSetting(string generator, string key, bool optional = false)
    {
        if (!this.GeneratorsConfig.ContainsKey(generator))
        {
            throw new ConfigurationException($"Embedding generator '{generator}' configuration not found");
        }

        if (!this.GeneratorsConfig[generator].ContainsKey(key))
        {
            if (optional)
            {
                return string.Empty;
            }

            throw new ConfigurationException($"Configuration '{generator}' is missing the '{key}' value");
        }

        return this.GeneratorsConfig[generator][key];
    }
}
