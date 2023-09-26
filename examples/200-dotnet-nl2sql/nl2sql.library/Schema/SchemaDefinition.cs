// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;

namespace SemanticKernel.Data.Nl2Sql.Library.Schema;

public sealed class SchemaDefinition
{
    public SchemaDefinition(
        string name,
        string platform,
        string? description,
        IEnumerable<SchemaTable> tables)
    {
        this.Name = name;
        this.Platform = platform;
        this.Description = description;
        this.Tables = tables ?? Array.Empty<SchemaTable>();
    }

    public string Name { get; }

    public string? Description { get; }

    public string Platform { get; }

    public IEnumerable<SchemaTable> Tables { get; }
}
