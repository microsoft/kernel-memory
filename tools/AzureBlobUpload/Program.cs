// Copyright (c) Microsoft. All rights reserved.

/*
 * Usage: dotnet run [file path]
 *
 * Example:
 *  dotnet run file4-KM-Readme.pdf
 *  upload.sh file4-KM-Readme.pdf
 *
 * For more advanced features, consider using azcopy: https://learn.microsoft.com/azure/storage/common/storage-ref-azcopy
 *
 * Env vars required:
 * - BLOB_CONN_STRING: Azure blob connection string
 * - BLOB_CONTAINER: name of the container where files are uploaded
 * - BLOB_PATH: name of the virtual folder where files are uploaded
 * - DOCUMENT_ID: name of the document folder containing the files
 *
 * You can store env vars under Properties/launchSettings.json, see the example below:{
    {
      "profiles": {
        "run": {
          "environmentVariables": {
            "ASPNETCORE_ENVIRONMENT": "Development",
            "BLOB_CONN_STRING": "DefaultEndpointsProtocol=https;AccountName=...FOO...;AccountKey=...KEY...;EndpointSuffix=core.windows.net",
            "BLOB_CONTAINER": "...NAME...",
            "BLOB_PATH": "/",
            "DOCUMENT_ID": "...NAME..."
          },
          "commandName": "Project",
          "launchBrowser": false
        }
      }
    }
 */

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.DocumentStorage.AzureBlobs;

if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
{
    Console.WriteLine("File path not specified. Provide the path of the file to upload.");
    Environment.Exit(-1);
}

var filePath = args[0];
if (Directory.Exists(filePath))
{
    Console.WriteLine($"The path provided is a directory, not a file: {filePath}");
    Environment.Exit(-2);
}

if (!File.Exists(filePath))
{
    Console.WriteLine($"File not found: {filePath}");
    Environment.Exit(-3);
}

var fileName = filePath.Replace('\\', '/').Split('/').Last();

Console.WriteLine("Uploading...");
await GetAzureBlobClient().WriteFileAsync(
    index: GetIndexName(),
    documentId: GetDocumentId(),
    fileName: fileName,
    streamContent: File.OpenRead(filePath)).ConfigureAwait(false);

Console.WriteLine($"File uploaded: {fileName}");

static AzureBlobsStorage GetAzureBlobClient()
{
    var blobConnString = Environment.GetEnvironmentVariable("BLOB_CONN_STRING");
    var blobContainer = Environment.GetEnvironmentVariable("BLOB_CONTAINER");

    if (string.IsNullOrWhiteSpace(blobConnString))
    {
        Console.WriteLine("BLOB_CONN_STRING env var not defined. Provide the Azure Blobs connection string.");
        Environment.Exit(-4);
    }

    if (string.IsNullOrWhiteSpace(blobContainer))
    {
        Console.WriteLine("BLOB_CONTAINER env var not defined. Provide the Azure Blobs container name.");
        Environment.Exit(-5);
    }

    return new AzureBlobsStorage(new AzureBlobsConfig
    {
        Auth = AzureBlobsConfig.AuthTypes.ConnectionString,
        ConnectionString = blobConnString,
        Container = blobContainer,
    });
}

static string GetIndexName()
{
    var indexName = Environment.GetEnvironmentVariable("BLOB_PATH");
    if (string.IsNullOrWhiteSpace(indexName))
    {
        Console.WriteLine("BLOB_PATH env var not defined. Provide the Azure Blobs container virtual folder name.");
        Environment.Exit(-6);
    }

    return indexName;
}

static string GetDocumentId()
{
    var id = Environment.GetEnvironmentVariable("DOCUMENT_ID");
    if (string.IsNullOrWhiteSpace(id))
    {
        Console.WriteLine("DOCUMENT_ID env var not defined. Provide the name of the document folder containing the files.");
        Environment.Exit(-6);
    }

    return id;
}
