// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.KernelMemory.DataFormats.Office;

public class MsPowerPointDecoderConfig
{
    /// <summary>
    /// Template used for the optional slide number added at the start of each slide.
    /// </summary>
    public string SlideNumberTemplate { get; set; } = "# Slide {number}";

    /// <summary>
    /// Template used for the optional text added at the end of each slide
    /// </summary>
    public string EndOfSlideMarkerTemplate { get; set; } = "# End of slide {number}";

    /// <summary>
    /// Whether to include the slide number before the text.
    /// </summary>
    public bool WithSlideNumber { get; set; } = true;

    /// <summary>
    /// Whether to add a marker after the text of each slide.
    /// </summary>
    public bool WithEndOfSlideMarker { get; set; } = false;

    /// <summary>
    /// Whether to skip hidden slides.
    /// </summary>
    public bool SkipHiddenSlides { get; set; } = true;
}
