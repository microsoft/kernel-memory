// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Microsoft.SemanticMemory.InteractiveSetup.Sections;

public static class ContentStorage
{
    public static void Setup()
    {
        SetupUI.AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "Where should the service store files?",
            Options = new List<Answer>
            {
                new("Azure Blobs", AzureBlobContentStorageSetup),
                new("Local file system", FileSystemContentStorageSetup),
                new("-exit-", SetupUI.Exit),
            }
        });
    }

    private static void AzureBlobContentStorageSetup()
    {
        JObject data = AppSettings.Load();

        data[Main.MemKey]![Main.StorageKey] = new JObject()
        {
            [Main.TypeKey] = "AzureBlobs",
            ["AzureBlobs"] = new JObject
            {
                [Main.ContainerNameKey] = SetupUI.AskOpenQuestion("Azure Blobs <container name>", data[Main.MemKey]?[Main.StorageKey]?["AzureBlobs"]?[Main.ContainerNameKey]?.ToString()),
                [Main.AccountNameKey] = SetupUI.AskOpenQuestion("Azure Blobs <account name>", data[Main.MemKey]?[Main.StorageKey]?["AzureBlobs"]?[Main.AccountNameKey]?.ToString()),
                [Main.ConnectionStringKey] = SetupUI.AskPassword("Azure Blobs <connection string>", data[Main.MemKey]?[Main.StorageKey]?["AzureBlobs"]?[Main.ConnectionStringKey]?.ToString()),
                [Main.AuthKey] = Main.ConnectionStringAuthType
            }
        };

        AppSettings.Save(data);
    }

    private static void FileSystemContentStorageSetup()
    {
        JObject data = AppSettings.Load();

        data[Main.MemKey]![Main.StorageKey] = new JObject
        {
            [Main.TypeKey] = "FileSystem",
            ["FileSystem"] = new JObject
            {
                ["Directory"] = SetupUI.AskOpenQuestion("Directory", data[Main.MemKey]?[Main.StorageKey]?["FileSystem"]?["Directory"]?.ToString())
            }
        };

        string path = data[Main.MemKey]?[Main.StorageKey]?["FileSystem"]?["Directory"]?.ToString()!;

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            if (!Directory.Exists(path))
            {
                throw new SetupException($"Unable to find/create directory {path}");
            }
        }

        AppSettings.Save(data);
    }
}
