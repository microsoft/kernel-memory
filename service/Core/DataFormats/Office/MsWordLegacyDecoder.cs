// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.Pipeline;
using Microsoft.KernelMemory.Text;
using NPOI.HWPF;
using NPOI.HWPF.Extractor;

namespace Microsoft.KernelMemory.DataFormats.Office;

[Experimental("KMEXP00")]
public sealed class MsWordLegacyDecoder : IContentDecoder
{
    private readonly ILogger<MsWordLegacyDecoder> _log;

    static MsWordLegacyDecoder()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); // Ensure legacy encodings are available: https://nicolaiarocci.com/how-to-read-windows-1252-encoded-files-with-.netcore-and-.net5-/
    }

    public MsWordLegacyDecoder(ILoggerFactory? loggerFactory = null)
    {
        this._log = (loggerFactory ?? DefaultLogger.Factory).CreateLogger<MsWordLegacyDecoder>();
    }

    /// <inheritdoc />
    public bool SupportsMimeType(string mimeType)
    {
        return mimeType != null && mimeType.StartsWith(MimeTypes.MsWord, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public Task<FileContent> DecodeAsync(string filename, CancellationToken cancellationToken = default)
    {
        using var stream = File.OpenRead(filename);
        return this.DecodeAsync(stream, cancellationToken);
    }

    /// <inheritdoc />
    public Task<FileContent> DecodeAsync(BinaryData data, CancellationToken cancellationToken = default)
    {
        using var stream = data.ToStream();
        return this.DecodeAsync(stream, cancellationToken);
    }

    /// <inheritdoc />
    public Task<FileContent> DecodeAsync(Stream data, CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Extracting text from MS Word legacy (.doc) file");

        var result = new FileContent(MimeTypes.PlainText);

        try
        {
            var document = new HWPFDocument(data);
            var extractor = new WordExtractor(document);

            string[] paragraphs = extractor.ParagraphText;

            int pageNumber = 1;
            var sb = new StringBuilder();

            foreach (string paragraph in paragraphs)
            {
                if (!string.IsNullOrWhiteSpace(paragraph))
                {
                    sb.AppendLineNix(paragraph.Trim());

                    // For legacy .doc files, we'll treat each significant paragraph break as a potential page break
                    // This is a simplified approach since .doc format doesn't have reliable page break detection
                    if (sb.Length > 2000) // Arbitrary chunk size
                    {
                        string content = sb.ToString().NormalizeNewlines(false);
                        result.Sections.Add(new Chunk(content, pageNumber, Chunk.Meta(sentencesAreComplete: true)));
                        sb.Clear();
                        pageNumber++;
                    }
                }
            }

            // Add any remaining content
            if (sb.Length > 0)
            {
                string content = sb.ToString().NormalizeNewlines(false);
                result.Sections.Add(new Chunk(content, pageNumber, Chunk.Meta(sentencesAreComplete: true)));
            }

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            this._log.LogError(ex, "Error extracting text from MS Word legacy file");
            throw;
        }
    }
}
