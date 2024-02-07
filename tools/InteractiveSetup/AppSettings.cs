// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.KernelMemory.InteractiveSetup;

public static class AppSettings
{
    private const string SettingsFile = "appsettings.Development.json";
    private const string DefaultSettingsFile = "appsettings.json";
    private static readonly JsonSerializerSettings s_jsonOptions = new() { Formatting = Formatting.Indented };

    public static void Change(Action<KernelMemoryConfig> configChanges)
    {
        CreateFileIfNotExists();

        KernelMemoryConfig config = GetCurrentConfig();

        configChanges.Invoke(config);

        string json = File.ReadAllText(SettingsFile);
        JObject? data = JsonConvert.DeserializeObject<JObject>(json);
        if (data == null)
        {
            throw new SetupException("Unable to parse file");
        }

        data["KernelMemory"] = JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(config));

        json = JsonConvert.SerializeObject(data, s_jsonOptions);
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

    public static KernelMemoryConfig GetCurrentConfig()
    {
        JObject data = GetGlobalConfig();
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

    private static JObject GetGlobalConfig()
    {
        string json = File.ReadAllText(SettingsFile);
        JObject? data = JsonConvert.DeserializeObject<JObject>(json);
        if (data == null)
        {
            throw new SetupException($"Unable to parse `{SettingsFile}` file");
        }

        // TODO: merge appsettings.json, only needed blocks
        // if (File.Exists(DefaultSettingsFile))
        // {
        //     json = File.ReadAllText(DefaultSettingsFile);
        //     JObject? defaultData = JsonConvert.DeserializeObject<JObject>(json);
        //     if (defaultData == null)
        //     {
        //         throw new SetupException($"Unable to parse `{DefaultSettingsFile}` file");
        //     }
        //
        //     defaultData.Merge(data, new JsonMergeSettings
        //     {
        //         MergeArrayHandling = MergeArrayHandling.Replace,
        //         PropertyNameComparison = StringComparison.OrdinalIgnoreCase,
        //     });
        //
        //     data = defaultData;
        // }

        return data;
    }

    private static void CreateFileIfNotExists()
    {
        if (File.Exists(SettingsFile)) { return; }

        File.Create(SettingsFile).Dispose();
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

        File.WriteAllText(SettingsFile, JsonConvert.SerializeObject(data, Formatting.Indented));
    }
}
