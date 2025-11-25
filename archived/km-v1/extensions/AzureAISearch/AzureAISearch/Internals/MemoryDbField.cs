// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.KernelMemory.MemoryDb.AzureAISearch;

internal sealed class MemoryDbField
{
#pragma warning disable CA1720
    public enum FieldType
    {
        Unknown = 0,
        Vector = 1,
        Text = 2,
        Integer = 3,
        Decimal = 4,
        Bool = 5,
        ListOfStrings = 6,
    }
#pragma warning restore CA1720

    public enum VectorMetricType
    {
        Cosine = 0,
        Euclidean = 1,
        DotProduct = 2,
    }

    public FieldType Type { get; set; } = FieldType.Unknown;
    public string Name { get; set; } = string.Empty;

    public bool IsKey { get; set; } = false;
    public bool IsFilterable { get; set; } = false;
    public int VectorSize { get; set; } = 0;
    public VectorMetricType VectorMetric { get; set; } = VectorMetricType.Cosine;
}
