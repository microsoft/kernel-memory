// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.SemanticKernel.SemanticMemory.Core.Pipeline;

public static class MimeTypes
{
    public const string PlainText = "text/plain";
    public const string MarkDown = "text/plain-markdown";
    public const string MsWord = "application/msword";
    public const string Pdf = "application/pdf";
}

public interface IMimeTypeDetection
{
    public string GetFileType(string filename);
}

public class MimeTypesDetection : IMimeTypeDetection
{
    public string GetFileType(string filename)
    {
        if (filename.EndsWith(".TXT", StringComparison.InvariantCultureIgnoreCase)) { return MimeTypes.PlainText; }

        if (filename.EndsWith(".MD", StringComparison.InvariantCultureIgnoreCase)) { return MimeTypes.MarkDown; }

        if (filename.EndsWith(".DOC", StringComparison.InvariantCultureIgnoreCase) || filename.EndsWith(".DOCX", StringComparison.InvariantCultureIgnoreCase)) { return MimeTypes.MsWord; }

        if (filename.EndsWith(".PDF", StringComparison.InvariantCultureIgnoreCase)) { return MimeTypes.Pdf; }

        throw new NotSupportedException($"File type not supported: {filename}");
    }
}
