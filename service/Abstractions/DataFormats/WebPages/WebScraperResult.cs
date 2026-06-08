// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.KernelMemory.DataFormats.WebPages;

public class WebScraperResult
{
    public BinaryData Content { get; set; } = new(string.Empty);
    public string ContentType { get; set; } = string.Empty;
    public bool Success { get; set; } = false;
    public string Error { get; set; } = string.Empty;
}
