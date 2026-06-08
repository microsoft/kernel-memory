// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.KernelMemory.MemoryDb.AzureAISearch;

internal sealed class MemoryDbSchema
{
    public List<MemoryDbField> Fields { get; set; } = [];

    public void Validate(bool vectorSizeRequired = false)
    {
        if (this.Fields.Count == 0)
        {
            throw new AzureAISearchMemoryException("The schema is empty", isTransient: false);
        }

        if (this.Fields.All(x => x.Type != MemoryDbField.FieldType.Vector))
        {
            throw new AzureAISearchMemoryException("The schema doesn't contain a vector field", isTransient: false);
        }

        int keys = this.Fields.Count(x => x.IsKey);
        switch (keys)
        {
            case 0:
                throw new AzureAISearchMemoryException("The schema doesn't contain a key field", isTransient: false);
            case > 1:
                throw new AzureAISearchMemoryException("The schema cannot contain more than one key", isTransient: false);
        }

        if (vectorSizeRequired && this.Fields.Any(x => x is { Type: MemoryDbField.FieldType.Vector, VectorSize: 0 }))
        {
            throw new AzureAISearchMemoryException("Vector fields must have a size greater than zero defined", isTransient: false);
        }

        if (this.Fields.Any(x => x is { Type: MemoryDbField.FieldType.Bool, IsKey: true }))
        {
            throw new AzureAISearchMemoryException("Boolean fields cannot be used as unique keys", isTransient: false);
        }

        if (this.Fields.Any(x => x is { Type: MemoryDbField.FieldType.ListOfStrings, IsKey: true }))
        {
            throw new AzureAISearchMemoryException("Collection fields cannot be used as unique keys", isTransient: false);
        }

        if (this.Fields.Any(x => x is { Type: MemoryDbField.FieldType.Vector, IsKey: true }))
        {
            throw new AzureAISearchMemoryException("Vector fields cannot be used as unique keys", isTransient: false);
        }
    }
}
