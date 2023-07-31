// Copyright (c) Microsoft. All rights reserved.

using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.SemanticMemory.InteractiveSetup;

public static class AppSettings
{
    private const string SettingsFile = "appsettings.Development.json";

    public static JObject Load()
    {
        CreateFileIfNotExists();
        JObject data = ReadJsonFile();

        if (data[Main.MemKey] == null)
        {
            data[Main.MemKey] = new JObject();
        }

        if (data[Main.MemKey]!["Service"] == null)
        {
            data[Main.MemKey]!["Service"] = new JObject();
            data[Main.MemKey]!["Service"]!["RunWebService"] = false;
            data[Main.MemKey]!["Service"]!["RunHandlers"] = false;
        }

        if (data[Main.MemKey]![Main.StorageKey] == null)
        {
            data[Main.MemKey]![Main.StorageKey] = new JObject();
        }

        if (data[Main.MemKey]!["Search"] == null)
        {
            data[Main.MemKey]!["Search"] = new JObject();
        }

        if (data[Main.MemKey]!["Search"]!["VectorDb"] == null)
        {
            data[Main.MemKey]!["Search"]!["VectorDb"] = new JObject();
        }

        if (data[Main.MemKey]!["Search"]!["EmbeddingGenerator"] == null)
        {
            data[Main.MemKey]!["Search"]!["EmbeddingGenerator"] = new JObject();
        }

        if (data[Main.MemKey]!["Search"]!["TextGenerator"] == null)
        {
            data[Main.MemKey]!["Search"]!["TextGenerator"] = new JObject();
        }

        if (data[Main.MemKey]![Main.OrchestrationKey] == null)
        {
            data[Main.MemKey]![Main.OrchestrationKey] = new JObject();
        }

        if (data[Main.MemKey]![Main.HandlersKey] == null)
        {
            data[Main.MemKey]![Main.HandlersKey] = new JObject();
        }

        if (data[Main.MemKey]![Main.HandlersKey]!["gen_embeddings"] == null)
        {
            data[Main.MemKey]![Main.HandlersKey]!["gen_embeddings"] = new JObject();
        }

        if (data[Main.MemKey]![Main.HandlersKey]!["save_embeddings"] == null)
        {
            data[Main.MemKey]![Main.HandlersKey]!["save_embeddings"] = new JObject();
        }

        return data;
    }

    public static void Save(JObject data)
    {
        var json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(SettingsFile, json);
    }

    private static JObject ReadJsonFile()
    {
        if (!File.Exists(SettingsFile))
        {
            throw new SetupException($"{SettingsFile} not found");
        }

        string json = File.ReadAllText(SettingsFile);
        if (string.IsNullOrEmpty(json))
        {
            return new JObject();
        }

        var data = JsonConvert.DeserializeObject<JObject>(json);
        if (data == null)
        {
            throw new SetupException($"Unable to parse JSON file {SettingsFile}");
        }

        return data;
    }

    private static void CreateFileIfNotExists()
    {
        if (!File.Exists(SettingsFile))
        {
            File.Create(SettingsFile).Dispose();
            File.WriteAllText(SettingsFile, "{}");
        }
    }
}
