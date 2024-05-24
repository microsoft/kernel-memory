// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;

namespace Microsoft.KernelMemory.Prompts;

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
[Experimental("KMEXP00")]
public sealed class EmbeddedPromptProvider : IPromptProvider
{
    private static readonly string? s_namespace = typeof(EmbeddedPromptProvider).Namespace;

    public string ReadPrompt(string promptName)
    {
        return ReadFile(promptName);
    }

    private static string ReadFile(string promptName)
    {
        var fileName = $"{promptName}.txt";

        // Get the current assembly. Note: this class is in the same assembly where the embedded resources are stored.
        Assembly? assembly = typeof(EmbeddedPromptProvider).GetTypeInfo().Assembly;
        if (assembly == null) { throw new ConfigurationException($"[{s_namespace}] Assembly not found, unable to load '{fileName}' resource"); }

        // Resources are mapped like types, using the namespace and appending "." (dot) and the file name
        var resourceName = $"{s_namespace}." + fileName;
        using Stream? resource = assembly.GetManifestResourceStream(resourceName);
        if (resource == null) { throw new ConfigurationException($"{resourceName} resource not found"); }

        // Return the resource content, in text format.
        using var reader = new StreamReader(resource);
        return reader.ReadToEnd();
    }
}
