// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.SemanticMemory.Pipeline;

public static class MimeTypes
{
    public const string PlainText = "text/plain";
    public const string MarkDown = "text/plain-markdown";
    public const string MsWord = "application/msword";
    public const string Pdf = "application/pdf";
    public const string Json = "application/json";
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
    public const string Pdf = ".pdf";
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

    public IEnumerable<string> GetFileTypes();
}

public class MimeTypesDetection : IMimeTypeDetection
{
    private static readonly IReadOnlyDictionary<string, string> extensionTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { FileExtensions.ImageBmp, MimeTypes.ImageBmp },
            { FileExtensions.ImageGif, MimeTypes.ImageGif },
            { FileExtensions.ImageJpeg, MimeTypes.ImageJpeg },
            { FileExtensions.ImageJpg, MimeTypes.ImageJpeg },
            { FileExtensions.ImagePng, MimeTypes.ImagePng },
            { FileExtensions.ImageTiff, MimeTypes.ImageTiff },
            { FileExtensions.Json, MimeTypes.Json },
            { FileExtensions.MarkDown, MimeTypes.MarkDown },
            { FileExtensions.MsWord, MimeTypes.MsWord },
            { FileExtensions.MsWordX, MimeTypes.MsWord },
            { FileExtensions.PlainText, MimeTypes.PlainText },
            { FileExtensions.Pdf, MimeTypes.Pdf },
            { FileExtensions.TextEmbeddingVector, MimeTypes.TextEmbeddingVector },
        };

    private readonly HashSet<string> supportedTypes;

    internal MimeTypesDetection(bool supportImage = false)
    {
        this.supportedTypes =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                FileExtensions.TextEmbeddingVector,
                FileExtensions.MarkDown,
                FileExtensions.MsWord,
                FileExtensions.MsWordX,
                FileExtensions.PlainText,
                FileExtensions.Pdf,
                FileExtensions.Json,
            };

        if (supportImage)
        {
            this.supportedTypes.UnionWith(
                new[]
                {
                    FileExtensions.ImageBmp,
                    FileExtensions.ImageGif,
                    FileExtensions.ImageJpeg,
                    FileExtensions.ImageJpg,
                    FileExtensions.ImagePng,
                    FileExtensions.ImageTiff,
                });
        }
    }

    public string GetFileType(string filename)
    {
        string extension = Path.GetExtension(filename);

        if (this.supportedTypes.Contains(extension) &&
            extensionTypes.TryGetValue(extension, out var mimeType))
        {
            return mimeType;
        }

        throw new NotSupportedException($"File type not supported: {filename}");
    }

    public IEnumerable<string> GetFileTypes()
    {
        return this.supportedTypes.ToArray();
    }
}
