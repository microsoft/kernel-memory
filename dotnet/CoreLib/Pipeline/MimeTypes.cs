// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.SemanticMemory.Pipeline;

public static class MimeTypes
{
    public const string PlainText = "text/plain";
    public const string MarkDown = "text/plain-markdown";
    public const string MsWord = "application/msword";
    public const string Pdf = "application/pdf";
    public const string TextEmbeddingVector = "float[]";
    public const string Json = "application/json";
}

public static class FileExtensions
{
    public const string PlainText = ".txt";
    public const string Json = ".json";
    public const string MarkDown = ".md";
    public const string MsWord = ".doc";
    public const string MsWordX = ".docx";
    public const string Pdf = ".pdf";
    public const string TextEmbeddingVector = ".text_embedding";
}

public interface IMimeTypeDetection
{
    public string GetFileType(string filename);

    public bool HasFileType(string filename);
}

public class MimeTypesDetection : IMimeTypeDetection
{
    private static readonly IReadOnlyDictionary<string, string> extensionTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { FileExtensions.PlainText, MimeTypes.PlainText },
            { FileExtensions.MarkDown, MimeTypes.MarkDown },
            { FileExtensions.Json, MimeTypes.Json },
            { FileExtensions.MsWord, MimeTypes.MsWord },
            { FileExtensions.MsWordX, MimeTypes.MsWord },
            { FileExtensions.Pdf, MimeTypes.Pdf },
            { FileExtensions.TextEmbeddingVector, MimeTypes.TextEmbeddingVector },
        };

    public string GetFileType(string filename)
    {
        string extension = Path.GetExtension(filename);

        if (extensionTypes.TryGetValue(extension, out var mimeType))
        {
            return mimeType;
        }

        throw new NotSupportedException($"File type not supported: {filename}");
    }

    public bool HasFileType(string filename)
    {
        string extension = Path.GetExtension(filename);

        return extensionTypes.ContainsKey(extension);
    }
}
