// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.KernelMemory.DataFormats;

public class DocumentPage
{
    /// <summary>
    /// Page text content
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Page number
    /// </summary>
    public int Number { get; }

    public DocumentPage(string? text, int number)
    {
        this.Number = number;
        this.Text = text ?? string.Empty;
    }
}
