// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using Text = DocumentFormat.OpenXml.Drawing.Text;

namespace Microsoft.SemanticMemory.DataFormats.Office;

public class MsPowerPointDecoder
{
    public string DocToText(string filename)
    {
        using var stream = File.OpenRead(filename);
        return this.DocToText(stream);
    }

    public string DocToText(BinaryData data)
    {
        using var stream = data.ToStream();
        return this.DocToText(stream);
    }

    public string DocToText(Stream data)
    {
        using var presentationDocument = PresentationDocument.Open(data, false);
        var sb = new StringBuilder();

        if (presentationDocument.PresentationPart is PresentationPart presentationPart
            && presentationPart.Presentation is Presentation presentation
            && presentation.SlideIdList is SlideIdList slideIdList
            && slideIdList.Elements<SlideId>().ToList() is List<SlideId> slideIds and { Count: > 0 })
        {
            foreach (var slideId in slideIds)
            {
                if ((string?)slideId.RelationshipId is string relationshipId
                    && presentationPart.GetPartById(relationshipId) is SlidePart slidePart
                    && slidePart.Slide.Descendants<Text>().ToList() is List<Text> texts and { Count: > 0 })
                {
                    for (var i = 0; i < texts.Count; i++)
                    {
                        var text = texts[i];

                        sb.Append(text.Text);

                        if (i < texts.Count - 1)
                        {
                            sb.Append(' ');
                        }
                    }

                    sb.AppendLine();
                }
            }
        }

        return sb.ToString().Trim();
    }
}
