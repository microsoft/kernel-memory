// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.SemanticMemory.Pipeline;

public static class MimeTypes
{
    public const string PlainText = "text/plain";
    public const string MarkDown = "text/plain-markdown";
    public const string MsWord = "application/msword";
    public const string Pdf = "application/pdf";
    public const string TextEmbeddingVector = "float[]";
    public const string Json = "application/json";
    public const string WebPageUrl = "text/x-uri";
}

public static class FileExtensions
{
    public const string PlainText = ".txt";
    public const string Json = ".json";
    public const string MarkDown = ".md";
    public const string MsWord = ".doc";
    public const string MsWordX = ".docx";
    public const string Pdf = ".pdf";
    public const string WebPageUrl = ".url";
    public const string TextEmbeddingVector = ".text_embedding";
}

public interface IMimeTypeDetection
{
    public string GetFileType(string filename);
}

public class MimeTypesDetection : IMimeTypeDetection
{
    public string GetFileType(string filename)
    {
        if (filename.EndsWith(FileExtensions.PlainText, StringComparison.InvariantCultureIgnoreCase))
        {
            return MimeTypes.PlainText;
        }

        if (filename.EndsWith(FileExtensions.MarkDown, StringComparison.InvariantCultureIgnoreCase))
        {
            return MimeTypes.MarkDown;
        }

        if (filename.EndsWith(FileExtensions.Json, StringComparison.InvariantCultureIgnoreCase))
        {
            return MimeTypes.Json;
        }

        if (filename.EndsWith(FileExtensions.MsWord, StringComparison.InvariantCultureIgnoreCase)
            || filename.EndsWith(FileExtensions.MsWordX, StringComparison.InvariantCultureIgnoreCase))
        {
            return MimeTypes.MsWord;
        }

        if (filename.EndsWith(FileExtensions.Pdf, StringComparison.InvariantCultureIgnoreCase))
        {
            return MimeTypes.Pdf;
        }

        if (filename.EndsWith(FileExtensions.TextEmbeddingVector, StringComparison.InvariantCultureIgnoreCase))
        {
            return MimeTypes.TextEmbeddingVector;
        }

        if (filename.EndsWith(FileExtensions.WebPageUrl, StringComparison.InvariantCultureIgnoreCase))
        {
            return MimeTypes.WebPageUrl;
        }

        throw new NotSupportedException($"File type not supported: {filename}");
    }
}
