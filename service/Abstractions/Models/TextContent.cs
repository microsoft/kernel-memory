// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.KernelMemory.Models;

public class TextContent
{
    public string Text { get; set; } = string.Empty;

    public TokenUsage? TokenUsage { get; set; }

    public TextContent(string text, TokenUsage? tokenUsage = null)
    {
        this.Text = text;
        this.TokenUsage = tokenUsage;
    }
}
