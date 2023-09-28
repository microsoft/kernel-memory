// Copyright (c) Microsoft. All rights reserved.

using System.IO;
using System.Threading.Tasks;

namespace SemanticKernel.Data.Nl2Sql.Library.Schema;

internal interface ISchemaFormatter
{
    Task WriteAsync(TextWriter writer, SchemaDefinition schema);
}
