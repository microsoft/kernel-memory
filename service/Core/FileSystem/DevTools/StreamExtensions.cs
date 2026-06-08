// Copyright (c) Microsoft. All rights reserved.

using System.IO;

namespace Microsoft.KernelMemory.FileSystem.DevTools;

internal static class StreamExtensions
{
    public static byte[] ReadAllBytes(this Stream stream)
    {
        if (stream is MemoryStream s1)
        {
            return s1.ToArray();
        }

        using (var s2 = new MemoryStream())
        {
            stream.CopyTo(s2);
            return s2.ToArray();
        }
    }
}
