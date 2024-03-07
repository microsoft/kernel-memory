// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.KernelMemory.Pipeline;

public static class MimeTypes
{
    public const string PlainText = "text/plain";
    public const string MarkDown = "text/plain-markdown";
    public const string Html = "text/html";
    public const string MsWord = "application/msword";
    public const string MsPowerPoint = "application/mspowerpoint";
    public const string MsExcel = "application/msexcel";
    public const string Pdf = "application/pdf";
    public const string Json = "application/json";
    public const string WebPageUrl = "text/x-uri";
    public const string TextEmbeddingVector = "float[]";
    public const string ImageBmp = "image/bmp";
    public const string ImageGif = "image/gif";
    public const string ImageJpeg = "image/jpeg";
    public const string ImagePng = "image/png";
    public const string ImageTiff = "image/tiff";
}

public static class FileExtensions
{
    public const string PlainText = ".txt";
    public const string Json = ".json";
    public const string MarkDown = ".md";
    public const string MsWord = ".doc";
    public const string MsWordX = ".docx";
    public const string MsPowerPoint = ".pptx";
    public const string MsExcel = ".xlsx";
    public const string Pdf = ".pdf";
    public const string Htm = ".htm";
    public const string Html = ".html";
    public const string WebPageUrl = ".url";
    public const string TextEmbeddingVector = ".text_embedding";
    public const string ImageBmp = ".bmp";
    public const string ImageGif = ".gif";
    public const string ImageJpeg = ".jpeg";
    public const string ImageJpg = ".jpg";
    public const string ImagePng = ".png";
    public const string ImageTiff = ".tiff";
}

public interface IMimeTypeDetection
{
    public string GetFileType(string filename);
}

public class MimeTypesDetection : IMimeTypeDetection
{
    private static readonly Dictionary<string, string> s_extensionTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { FileExtensions.ImageBmp, MimeTypes.ImageBmp },
            { FileExtensions.ImageGif, MimeTypes.ImageGif },
            { FileExtensions.ImageJpeg, MimeTypes.ImageJpeg },
            { FileExtensions.ImageJpg, MimeTypes.ImageJpeg },
            { FileExtensions.ImagePng, MimeTypes.ImagePng },
            { FileExtensions.ImageTiff, MimeTypes.ImageTiff },
            { FileExtensions.Json, MimeTypes.Json },
            { FileExtensions.MarkDown, MimeTypes.MarkDown },
            { FileExtensions.Htm, MimeTypes.Html },
            { FileExtensions.Html, MimeTypes.Html },
            { FileExtensions.WebPageUrl, MimeTypes.WebPageUrl },
            { FileExtensions.MsWord, MimeTypes.MsWord },
            { FileExtensions.MsWordX, MimeTypes.MsWord },
            { FileExtensions.MsPowerPoint, MimeTypes.MsPowerPoint },
            { FileExtensions.MsExcel, MimeTypes.MsExcel },
            { FileExtensions.PlainText, MimeTypes.PlainText },
            { FileExtensions.Pdf, MimeTypes.Pdf },
            { FileExtensions.TextEmbeddingVector, MimeTypes.TextEmbeddingVector },
        };

    public string GetFileType(string filename)
    {
        string extension = Path.GetExtension(filename);

        if (s_extensionTypes.TryGetValue(extension, out var mimeType))
        {
            return mimeType;
        }

        throw new NotSupportedException($"File type not supported: {filename}");
    }
}
