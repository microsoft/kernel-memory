// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
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

public class MsPowerPointDecoder : IContentDecoder
{
    private readonly MsPowerPointConfig _config;
    private readonly ILogger<MsPowerPointDecoder> _log;

    public IEnumerable<string> SupportedMimeTypes { get; } = [MimeTypes.MsPowerPointX, MimeTypes.MsPowerPoint];

    public MsPowerPointDecoder(MsPowerPointConfig? config = null, ILogger<MsPowerPointDecoder>? log = null)
    {
        this._config = config ?? new MsPowerPointConfig();
        this._log = log ?? DefaultLogger<MsPowerPointDecoder>.Instance;
    }

    public Task<FileContent?> ExtractContentAsync(string handlerStepName, DataPipeline.FileDetails file, string filename, CancellationToken cancellationToken = default)
    {
        using var stream = File.OpenRead(filename);
        return this.ExtractContentAsync(handlerStepName, file, stream, cancellationToken);
    }

    public Task<FileContent?> ExtractContentAsync(string handlerStepName, DataPipeline.FileDetails file, BinaryData data, CancellationToken cancellationToken = default)
    {
        using var stream = data.ToStream();
        return this.ExtractContentAsync(handlerStepName, file, stream, cancellationToken);
    }

    public Task<FileContent?> ExtractContentAsync(string handlerStepName, DataPipeline.FileDetails file, Stream data, CancellationToken cancellationToken = default)
    {
        if (file.MimeType == MimeTypes.MsPowerPoint)
        {
            file.Log(
                handlerStepName,
                "Office 97-2003 format not supported. It is recommended to migrate to the newer OpenXML format (pptx). Ignoring the file."
            );

            this._log.LogWarning("Office 97-2003 file MIME type not supported: {0} - ignoring the file {1}", file.MimeType, file.Name);
            return Task.FromResult<FileContent?>(null);
        }

        this._log.LogDebug("Extracting text from MS PowerPoint file {0}", file.Name);

        var result = new FileContent();

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

        return Task.FromResult(result)!;
    }
}
