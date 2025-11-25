// Copyright (c) Microsoft. All rights reserved.

using System.Text;

namespace Microsoft.KernelMemory.Chunkers.internals;

internal class ChunkBuilder
{
    public readonly StringBuilder FullContent = new();
    public readonly StringBuilder NextSentence = new();
}
