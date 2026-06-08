// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Security.Cryptography;

namespace Microsoft.KernelMemory.Extensions;

internal static class BinaryDataExtensions
{
    public static string CalculateSHA256(this BinaryData binaryData)
    {
        byte[] byteArray = SHA256.HashData(binaryData.ToMemory().Span);
        return Convert.ToHexString(byteArray).ToLowerInvariant();
    }
}
