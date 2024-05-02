// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.Pipeline;

namespace Microsoft.KernelMemory.DataFormats.Office;

[Experimental("KMEXP00")]
public sealed class MsPowerPointDecoder : IContentDecoder
{
    private readonly MsPowerPointDecoderConfig _config;
    private readonly ILogger<MsPowerPointDecoder> _log;

    public MsPowerPointDecoder(MsPowerPointDecoderConfig? config = null, ILogger<MsPowerPointDecoder>? log = null)
    {
        this._config = config ?? new MsPowerPointDecoderConfig();
        this._log = log ?? DefaultLogger<MsPowerPointDecoder>.Instance;
    }

    /// <inheritdoc />
    public bool SupportsMimeType(string mimeType)
    {
        return mimeType != null && mimeType.StartsWith(MimeTypes.MsPowerPointX, StringComparison.OrdinalIgnoreCase);
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
        this._log.LogDebug("Extracting text from MS PowerPoint file");

        var result = new FileContent(MimeTypes.PlainText);
        using PresentationDocument presentationDocument = PresentationDocument.Open(data, false);
        var sb = new StringBuilder();

        if (presentationDocument.PresentationPart is PresentationPart presentationPart
            && presentationPart.Presentation is Presentation presentation
            && presentation.SlideIdList is SlideIdList slideIdList
            && slideIdList.Elements<SlideId>().ToList() is List<SlideId> slideIds and { Count: > 0 })
        {
            var slideNumber = 0;
            foreach (SlideId slideId in slideIds)
            {
                slideNumber++;
#pragma warning disable CA1508 // code taken from official MS docs
                if ((string?)slideId.RelationshipId is string relationshipId
                    && presentationPart.GetPartById(relationshipId) is SlidePart slidePart
                    && slidePart != null
                    && slidePart.Slide?.Descendants<DocumentFormat.OpenXml.Drawing.Text>().ToList() is List<DocumentFormat.OpenXml.Drawing.Text> texts and { Count: > 0 })
#pragma warning restore CA1508
                {
                    // Check if the slide is hidden and whether to skip it
                    // PowerPoint does not set the value of this property, in general, unless the slide is to be hidden
                    // The only way the Show property would exist and have a value of true would be if the slide had been hidden and then unhidden
                    // - Show is null: default, slide is visible
                    // - Show is false: the slide is hidden
                    // - Show is true: the slide is visible
                    bool isVisible = slidePart.Slide.Show ?? true;
                    if (this._config.SkipHiddenSlides && !isVisible) { continue; }

                    var currentSlideContent = new StringBuilder();
                    for (var i = 0; i < texts.Count; i++)
                    {
                        var text = texts[i];
                        currentSlideContent.Append(text.Text);
                        if (i < texts.Count - 1)
                        {
                            currentSlideContent.Append(' ');
                        }
                    }

                    // Skip the slide if there is no text
                    if (currentSlideContent.Length < 1) { continue; }

                    // Prepend slide number before the slide text
                    if (this._config.WithSlideNumber)
                    {
                        sb.AppendLine(this._config.SlideNumberTemplate.Replace("{number}", $"{slideNumber}", StringComparison.OrdinalIgnoreCase));
                    }

                    sb.Append(currentSlideContent);
                    sb.AppendLine();

                    // Append the end of slide marker
                    if (this._config.WithEndOfSlideMarker)
                    {
                        sb.AppendLine(this._config.EndOfSlideMarkerTemplate.Replace("{number}", $"{slideNumber}", StringComparison.OrdinalIgnoreCase));
                    }
                }

                string slideContent = sb.ToString().Trim();
                sb.Clear();
                result.Sections.Add(new FileSection(slideNumber, slideContent, true));
            }
        }

        return Task.FromResult(result);
    }
}
