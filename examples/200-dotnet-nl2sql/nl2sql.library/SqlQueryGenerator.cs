// Copyright (c) Microsoft. All rights reserved.

using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Orchestration;
using SemanticKernel.Data.Nl2Sql.Library.Internal;
using SemanticKernel.Data.Nl2Sql.Library.Schema;

namespace SemanticKernel.Data.Nl2Sql.Library;

/// <summary>
/// Generate SQL query targeting Microsoft SQL Server.
/// </summary>
public sealed class SqlQueryGenerator
{
    public const double DefaultMinRelevance = 0.7D;

    public const string ContextParamObjective = "data_objective";
    public const string ContextParamSchema = "data_schema";
    public const string ContextParamSchemaId = "data_schema_id";
    public const string ContextParamQuery = "data_query";
    public const string ContextParamPlatform = "data_platform";
    public const string ContextParamError = "data_error";

    private const string ContentLabelQuery = "sql";
    private const string ContentLabelAnswer = "answer";
    private const string ContentAffirmative = "yes";

    private const string SkillName = "nl2sql";

    private readonly double _minRelevanceScore;
    private readonly ISKFunction _promptEval;
    private readonly ISKFunction _promptGenerator;
    private readonly ISemanticTextMemory _memory;

    public SqlQueryGenerator(
        IKernel kernel,
        ISemanticTextMemory memory,
        string rootSkillFolder,
        double minRelevanceScore = DefaultMinRelevance)
    {
        var functions = kernel.ImportSemanticFunctionsFromDirectory(rootSkillFolder, SkillName);
        this._promptEval = functions["isquery"];
        this._promptGenerator = functions["generatequery"];
        this._memory = memory;
        this._minRelevanceScore = minRelevanceScore;

        kernel.ImportFunctions(this, SkillName);
    }

    /// <summary>
    /// Attempt to produce a query for the given objective based on the registered schemas.
    /// </summary>
    /// <param name="objective">A natural language objective</param>
    /// <param name="context">A <see cref="SKContext"/> object</param>
    /// <returns>A SQL query (or null if not able)</returns>
    [SKFunction, Description("Generate a data query for a given objective and schema")]
    [SKName("GenerateQueryFromObjective")]
    public async Task<string?> SolveObjectiveAsync(string objective, SKContext context)
    {
        // Search for schema with best similarity match to the objective
        var recall =
            await this._memory.SearchAsync(
                SchemaProvider.MemoryCollectionName,
                objective,
                limit: 1, // Take top result with maximum relevance (> minRelevanceScore)
                minRelevanceScore: this._minRelevanceScore,
                withEmbeddings: true).ToArrayAsync().ConfigureAwait(false);

        var best = recall.FirstOrDefault();
        if (best == null)
        {
            return null; // No schema / no query
        }

        var schemaName = best.Metadata.Id;
        var schemaText = best.Metadata.Text;
        var sqlPlatform = best.Metadata.AdditionalMetadata;

        context.Variables[ContextParamObjective] = objective;
        context.Variables[ContextParamSchema] = schemaText;
        context.Variables[ContextParamSchemaId] = schemaName;
        context.Variables[ContextParamPlatform] = sqlPlatform;

        // Screen objective to determine if it can be solved with the selected schema.
        if (!await this.ScreenObjectiveAsync(context).ConfigureAwait(false))
        {
            return null; // Objective doesn't pass screen
        }

        // Generate query
        await this._promptGenerator.InvokeAsync(context).ConfigureAwait(false);

        // Parse result to handle 
        return context.GetResult(ContentLabelQuery);
    }

    private async Task<bool> ScreenObjectiveAsync(SKContext context)
    {
        await this._promptEval.InvokeAsync(context).ConfigureAwait(false);

        var answer = context.GetResult(ContentLabelAnswer);

        return answer.Equals(ContentAffirmative, StringComparison.OrdinalIgnoreCase);
    }
}
