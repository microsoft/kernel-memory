// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using Microsoft.SemanticMemory.Core20;

namespace Microsoft.SemanticMemory.Core.MemoryStorage;

public class VectorDbField
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

public class VectorDbSchema
{
    public List<VectorDbField> Fields { get; set; } = new();

    public void Validate(bool vectorSizeRequired = false)
    {
        if (this.Fields.Count == 0)
        {
            throw new SemanticMemoryException("The schema is empty");
        }

        if (this.Fields.All(x => x.Type != VectorDbField.FieldType.Vector))
        {
            throw new SemanticMemoryException("The schema doesn't contain a vector field");
        }

        int keys = this.Fields.Count(x => x.IsKey);
        switch (keys)
        {
            case 0:
                throw new AzureCognitiveSearchMemoryException("The schema doesn't contain a key field");
            case > 1:
                throw new AzureCognitiveSearchMemoryException("The schema cannot contain more than one key");
        }

        if (vectorSizeRequired && this.Fields.Any(x => x is { Type: VectorDbField.FieldType.Vector, VectorSize: 0 }))
        {
            throw new AzureCognitiveSearchMemoryException("Vector fields must have a size greater than zero defined");
        }

        if (this.Fields.Any(x => x is { Type: VectorDbField.FieldType.Bool, IsKey: true }))
        {
            throw new AzureCognitiveSearchMemoryException("Boolean fields cannot be used as unique keys");
        }

        if (this.Fields.Any(x => x is { Type: VectorDbField.FieldType.ListOfStrings, IsKey: true }))
        {
            throw new AzureCognitiveSearchMemoryException("Collection fields cannot be used as unique keys");
        }

        if (this.Fields.Any(x => x is { Type: VectorDbField.FieldType.Vector, IsKey: true }))
        {
            throw new AzureCognitiveSearchMemoryException("Vector fields cannot be used as unique keys");
        }
    }
}
