// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.KernelMemory.MemoryDb.Qdrant;

internal static class QdrantConstants
{
    // Qdrant points properties
    public const string PointIdField = "id";
    public const string PointVectorField = "vector";
    public const string PointPayloadField = "payload";

    // Custom payload properties
    public const string PayloadIdField = "id";
    public const string PayloadTagsField = "tags";
    public const string PayloadPayloadField = "payload";
}
