// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.KernelMemory;

#pragma warning disable CA2225
public class GeneratedTextContent
{
    public string Text { get; set; }

    public TokenUsage? TokenUsage { get; set; }

    public GeneratedTextContent(string text, TokenUsage? tokenUsage = null)
    {
        this.Text = text;
        this.TokenUsage = tokenUsage;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return this.Text;
    }

    /// <summary>
    /// Convert a string to an instance of GeneratedTextContent
    /// </summary>
    /// <param name="text">Text content</param>
    public static implicit operator GeneratedTextContent(string text)
    {
        return new GeneratedTextContent(text);
    }
}
