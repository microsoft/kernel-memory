// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.KernelMemory.Models;

/// <summary>
/// A list of files and streams, organized to guarantee a unique name, and ready for upload.
/// </summary>
public class FileCollection
{
    /// <summary>
    /// List of files (to upload).
    /// Key = unique file name, including path.
    /// Value = file name without path, can be different from the original if different folders contain a file with the same name.
    /// </summary>
    private readonly Dictionary<string, string> _filePaths = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// List of streams to upload.
    /// Key = unique file name, no path.
    /// Value = content.
    /// </summary>
    private readonly Dictionary<string, Stream> _streams = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// List of unique file names, without path
    /// </summary>
    private readonly HashSet<string> _fileNames = new(StringComparer.OrdinalIgnoreCase);

    public void AddFile(string filePath)
    {
        if (this._filePaths.ContainsKey(filePath)) { return; }

        if (!File.Exists(filePath))
        {
            throw new KernelMemoryException($"File not found: '{filePath}'");
        }

        var file = new FileInfo(filePath);
        var fileName = file.Name;
        if (this._fileNames.Contains(fileName))
        {
            var count = 0;

            // Note: anonymize the path. This value will be visible in the storage service.
            var dirNameId = CalculateSHA256(Document.ReplaceInvalidChars(file.DirectoryName));
            do
            {
                // Prepend a unique ID (do not append, to avoid changing the file extension)
                fileName = $"{dirNameId}{count++}_{file.Name}";
            } while (this._fileNames.Contains(fileName));
        }

        this._filePaths.Add(filePath, fileName);
        this._fileNames.Add(fileName);
    }

    public void AddStream(string? fileName, Stream content)
    {
        if (content == null)
        {
            throw new KernelMemoryException("The content stream is NULL");
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = "content.txt";
        }

        var count = 0;
        while (this._fileNames.Contains(fileName!))
        {
            fileName = $"stream{count++}_{fileName}";
        }

        this._streams.Add(fileName!, content);
        this._fileNames.Add(fileName!);
    }

    public IEnumerable<(string name, Stream content)> GetStreams()
    {
        foreach (KeyValuePair<string, string> file in this._filePaths)
        {
            byte[] bytes = File.ReadAllBytes(file.Key);
            var data = new BinaryData(bytes);
            yield return (file.Value, data.ToStream());
        }

        foreach (KeyValuePair<string, Stream> stream in this._streams)
        {
            yield return (stream.Key, stream.Value);
        }
    }

#pragma warning disable CA1308 // lowercase is safe here and better for accessibility in external tools
    /// <summary>
    /// .NET Core 2.0 SHA256 string generator
    /// </summary>
    /// <param name="value">String to hash</param>
    /// <returns>Hash value</returns>
    private static string CalculateSHA256(string value)
    {
        byte[] byteArray;

#pragma warning disable CA1031 // ok to catch all
        try
        {
            byteArray = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        }
        catch (Exception)
        {
            return "SHA256Exception";
        }
#pragma warning restore CA1031

        return ToHexString(byteArray).ToLowerInvariant();
    }
#pragma warning restore CA1308

    /// <summary>
    /// .NET Core 2.0 equivalent of Convert.ToHexString
    /// </summary>
    public static string ToHexString(byte[] byteArray)
    {
        StringBuilder hex = new(byteArray.Length * 2);
        foreach (byte b in byteArray) { hex.AppendFormat(CultureInfo.InvariantCulture, "{0:x2}", b); }

        return hex.ToString();
    }
}
