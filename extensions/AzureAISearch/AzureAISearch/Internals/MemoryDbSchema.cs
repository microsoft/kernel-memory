// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.KernelMemory.MemoryDb.AzureAISearch;

internal sealed class MemoryDbSchema
{
    public List<MemoryDbField> Fields { get; set; } = new();

    public void Validate(bool vectorSizeRequired = false)
    {
        if (this.Fields.Count == 0)
        {
            throw new KernelMemoryException("The schema is empty");
        }

        if (this.Fields.All(x => x.Type != MemoryDbField.FieldType.Vector))
        {
            throw new KernelMemoryException("The schema doesn't contain a vector field");
        }

        int keys = this.Fields.Count(x => x.IsKey);
        switch (keys)
        {
            case 0:
                throw new KernelMemoryException("The schema doesn't contain a key field");
            case > 1:
                throw new KernelMemoryException("The schema cannot contain more than one key");
        }

        if (vectorSizeRequired && this.Fields.Any(x => x is { Type: MemoryDbField.FieldType.Vector, VectorSize: 0 }))
        {
            throw new KernelMemoryException("Vector fields must have a size greater than zero defined");
        }

        if (this.Fields.Any(x => x is { Type: MemoryDbField.FieldType.Bool, IsKey: true }))
        {
            throw new KernelMemoryException("Boolean fields cannot be used as unique keys");
        }

        if (this.Fields.Any(x => x is { Type: MemoryDbField.FieldType.ListOfStrings, IsKey: true }))
        {
            throw new KernelMemoryException("Collection fields cannot be used as unique keys");
        }

        if (this.Fields.Any(x => x is { Type: MemoryDbField.FieldType.Vector, IsKey: true }))
        {
            throw new KernelMemoryException("Vector fields cannot be used as unique keys");
        }
    }
}
