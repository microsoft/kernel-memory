// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using Microsoft.SemanticMemory.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.SemanticMemory.InteractiveSetup;

public static class AppSettings
{
    private const string SettingsFile = "appsettings.Development.json";

    public static void Change(Action<SemanticMemoryConfig> configChanges)
    {
        CreateFileIfNotExists();

        SemanticMemoryConfig config = GetCurrentConfig();

        configChanges.Invoke(config);

        string json = File.ReadAllText(SettingsFile);
        JObject? data = JsonConvert.DeserializeObject<JObject>(json);
        if (data == null)
        {
            throw new SetupException("Unable to parse file");
        }

        data["SemanticMemory"] = JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(config));

        json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(SettingsFile, json);
    }

    public static void GlobalChange(Action<JObject> configChanges)
    {
        CreateFileIfNotExists();

        JObject config = GetGlobalConfig();

        configChanges.Invoke(config);

        var json = JsonConvert.SerializeObject(config, Formatting.Indented);
        File.WriteAllText(SettingsFile, json);
    }

    public static SemanticMemoryConfig GetCurrentConfig()
    {
        JObject data = GetGlobalConfig();
        if (data["SemanticMemory"] == null)
        {
            Console.WriteLine("SemanticMemory property missing, using an empty configuration.");
            return new SemanticMemoryConfig();
        }

        SemanticMemoryConfig? config = JsonConvert
            .DeserializeObject<SemanticMemoryConfig>(JsonConvert
                .SerializeObject(data["SemanticMemory"]));
        if (config == null)
        {
            throw new SetupException("Unable to parse file");
        }

        return config;
    }

    private static JObject GetGlobalConfig()
    {
        string json = File.ReadAllText(SettingsFile);
        JObject? data = JsonConvert.DeserializeObject<JObject>(json);
        if (data == null)
        {
            throw new SetupException("Unable to parse file");
        }

        return data;
    }

    private static void CreateFileIfNotExists()
    {
        if (File.Exists(SettingsFile)) { return; }

        File.Create(SettingsFile).Dispose();
        var data = new
        {
            SemanticMemory = new
            {
            },
            Logging = new
            {
                LogLevel = new
                {
                    Default = "Information",
                }
            },
            AllowedHosts = "*",
        };

        File.WriteAllText(SettingsFile, JsonConvert.SerializeObject(data, Formatting.Indented));
    }
}
