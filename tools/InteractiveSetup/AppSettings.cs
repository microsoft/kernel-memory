// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.KernelMemory.InteractiveSetup;

/// <summary>
/// Handle settings stored in appsettings.development.json
/// </summary>
internal static class AppSettings
{
    private const string DefaultSettingsFile = "appsettings.json";
    private const string DevelopmentSettingsFile = "appsettings.Development.json";
    private static readonly JsonSerializerSettings s_jsonOptions = new() { Formatting = Formatting.Indented };

    public static void Change(Action<KernelMemoryConfig> configChanges)
    {
        CreateFileIfNotExists();

        KernelMemoryConfig config = GetCurrentConfig();

        configChanges.Invoke(config);

        string json = File.ReadAllText(DevelopmentSettingsFile);
        JObject? data = JsonConvert.DeserializeObject<JObject>(json);
        if (data == null)
        {
            throw new SetupException("Unable to parse file");
        }

        data["KernelMemory"] = JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(config));

        json = JsonConvert.SerializeObject(data, s_jsonOptions);
        File.WriteAllText(DevelopmentSettingsFile, json);
    }

    public static void GlobalChange(Action<JObject> configChanges)
    {
        CreateFileIfNotExists();

        JObject config = GetGlobalConfig();

        configChanges.Invoke(config);

        var json = JsonConvert.SerializeObject(config, Formatting.Indented);
        File.WriteAllText(DevelopmentSettingsFile, json);
    }

    /// <summary>
    /// Load current configuration from current folder, merging appsettings.json (if present) with appsettings.development.json
    /// Note: the code reads from the current folder, which is usually service/Service. Using ConfigurationBuilder would read from
    ///       bin/Debug/net7.0/, causing problems because GetGlobalConfig doesn't.
    /// </summary>
    public static KernelMemoryConfig GetCurrentConfig()
    {
        JObject data = GetGlobalConfig(true);
        if (data["KernelMemory"] == null)
        {
            Console.WriteLine("KernelMemory property missing, using an empty configuration.");
            return new KernelMemoryConfig();
        }

        KernelMemoryConfig? config = JsonConvert
            .DeserializeObject<KernelMemoryConfig>(JsonConvert
                .SerializeObject(data["KernelMemory"]));
        if (config == null)
        {
            throw new SetupException("Unable to parse file");
        }

        return config;
    }

    private static JObject GetGlobalConfig(bool includeDefaults = false)
    {
        if (!File.Exists(DevelopmentSettingsFile))
        {
            CreateFileIfNotExists();
        }

        string json = File.ReadAllText(DevelopmentSettingsFile);
        JObject? data = JsonConvert.DeserializeObject<JObject>(json);
        if (data == null)
        {
            throw new SetupException($"Unable to parse `{DevelopmentSettingsFile}` file");
        }

        if (includeDefaults && File.Exists(DefaultSettingsFile))
        {
            json = File.ReadAllText(DefaultSettingsFile);
            JObject? defaultData = JsonConvert.DeserializeObject<JObject>(json);
            if (defaultData == null)
            {
                throw new SetupException($"Unable to parse `{DefaultSettingsFile}` file");
            }

            defaultData.Merge(data, new JsonMergeSettings
            {
                MergeArrayHandling = MergeArrayHandling.Replace,
                PropertyNameComparison = StringComparison.OrdinalIgnoreCase,
            });

            data = defaultData;
        }

        return data;
    }

    private static void CreateFileIfNotExists()
    {
        if (File.Exists(DevelopmentSettingsFile)) { return; }

        File.Create(DevelopmentSettingsFile).Dispose();
        var data = new
        {
            KernelMemory = new
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

        File.WriteAllText(DevelopmentSettingsFile, JsonConvert.SerializeObject(data, Formatting.Indented));
    }
}
