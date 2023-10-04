﻿// Copyright (c) Microsoft. All rights reserved.

#define DISABLEHOST // Comment line to enable
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using SemanticKernel.Data.Nl2Sql.Library.Schema;
using Xunit;
using Xunit.Abstractions;

namespace SemanticKernel.Data.Nl2Sql.Harness;

/// <summary>
/// Harness for utilizing <see cref="SqlSchemaProvider"/> to capture live database schema
/// definitions: <see cref="SchemaDefinition"/>.
/// </summary>
public sealed class SqlSchemaProviderHarness
{
#if DISABLEHOST
    private const string SkipReason = "Harness only for local/dev environment";
#else
    private const string SkipReason = null;
#endif

    private const string DatabaseAdventureWorksLt = "AdventureWorksLT";
    private const string DatabaseDescriptionTest = "DescriptionTest";

    private readonly ITestOutputHelper _output;

    public SqlSchemaProviderHarness(ITestOutputHelper output)
    {
        this._output = output;
    }

    /// <summary>
    /// Reverse engineer live database (design-time task).
    /// </summary>
    /// <remarks>
    /// After testing with the sample data-sources, try one of your own!
    /// </remarks>
    [Fact(Skip = SkipReason)]
    public async Task ReverseEngineerSchemaAsync()
    {
        await this.CaptureSchemaAsync(
            DatabaseAdventureWorksLt,
            "Product, sales, and customer data for the AdventureWorks company.").ConfigureAwait(false);

        await this.CaptureSchemaAsync(
            DatabaseDescriptionTest,
            "Associates registered users with interest categories.").ConfigureAwait(false);

        // TODO: Reverse engineer your own database (comment-out others)
        //       Pass in optional 'tableNames' parameter to limit which tables or views are described.
    }

    private async Task CaptureSchemaAsync(string databaseKey, string? description, params string[] tableNames)
    {
        var connectionString = Harness.Configuration.GetConnectionString(databaseKey);
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        var provider = new SqlSchemaProvider(connection);

        var schema = await provider.GetSchemaAsync(description, tableNames).ConfigureAwait(false);

        await connection.CloseAsync().ConfigureAwait(false);

        // Capture YAML for inspection
        var yamlText = await schema.FormatAsync(YamlSchemaFormatter.Instance).ConfigureAwait(false);
        await this.SaveSchemaAsync("yaml", databaseKey, yamlText).ConfigureAwait(false);

        // Capture json for reserialization
        await this.SaveSchemaAsync("json", databaseKey, schema.ToJson()).ConfigureAwait(false);
    }

    private async Task SaveSchemaAsync(string extension, string databaseKey, string schemaText)
    {
        var fileName = Path.Combine(Repo.RootConfigFolder, "schema", $"{databaseKey}.{extension}");

        using var streamCompact =
            new StreamWriter(
                fileName,
                new FileStreamOptions
                {
                    Access = FileAccess.Write,
                    Mode = FileMode.Create,
                });

        await streamCompact.WriteAsync(schemaText).ConfigureAwait(false);
    }
}
