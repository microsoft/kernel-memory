// Copyright (c) Microsoft. All rights reserved.

using System.Text;

namespace Microsoft.KernelMemory.Chunkers.internals;

internal class Fragment
{
    public readonly string Content;
    public readonly bool IsSeparator;

    public Fragment(char content, bool isSeparator)
    {
        this.Content = content.ToString();
        this.IsSeparator = isSeparator;
    }

    public Fragment(string content, bool isSeparator)
    {
        this.Content = content;
        this.IsSeparator = isSeparator;
    }

    public Fragment(StringBuilder content, bool isSeparator)
    {
        this.Content = content.ToString();
        this.IsSeparator = isSeparator;
    }
}
