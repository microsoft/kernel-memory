// Copyright (c) Microsoft. All rights reserved.

using System.IO;
using System.Reflection;
using Microsoft.KernelMemory.Configuration;

namespace Microsoft.KernelMemory.AI.OpenAI.GPT3;

/// <summary>
/// Resource helper to load resources embedded in the assembly. By default we embed only
/// text files, so the helper is limited to returning text.
///
/// You can find information about embedded resources here:
/// * https://learn.microsoft.com/dotnet/core/extensions/create-resource-files
/// * https://learn.microsoft.com/dotnet/api/system.reflection.assembly.getmanifestresourcestream?view=net-7.0
///
/// To know which resources are embedded, check the csproj file.
/// </summary>
internal static class EmbeddedTokenizersGPT3Resource
{
    private static readonly string? s_namespace = typeof(EmbeddedTokenizersGPT3Resource).Namespace;

    /// <summary>
    /// Return content of BPE file.
    /// </summary>
    internal static string ReadBytePairEncodingTable()
    {
        return Read("vocab.bpe");
    }

    /// <summary>
    /// Return content of encoding table file.
    /// </summary>
    internal static string ReadEncodingTable()
    {
        return Read("encoder.json");
    }

    private static string Read(string fileName)
    {
        // Get the current assembly. Note: this class is in the same assembly where the embedded resources are stored.
        Assembly? assembly = typeof(EmbeddedTokenizersGPT3Resource).GetTypeInfo().Assembly;
        if (assembly == null) { throw new ConfigurationException($"[{s_namespace}] {fileName} assembly not found"); }

        // Resources are mapped like types, using the namespace and appending "." (dot) and the file name
        var resourceName = $"{s_namespace}." + fileName;
        using Stream? resource = assembly.GetManifestResourceStream(resourceName);
        if (resource == null) { throw new ConfigurationException($"{resourceName} resource not found"); }

        // Return the resource content, in text format.
        using var reader = new StreamReader(resource);
        return reader.ReadToEnd();
    }
}
