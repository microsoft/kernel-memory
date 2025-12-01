// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Core.Search.Query.Ast;
using KernelMemory.Core.Search.Query.Parsers;

namespace KernelMemory.Core.Tests.Search.Query;

/// <summary>
/// Tests to ensure InfixQueryParser and MongoJsonQueryParser produce equivalent ASTs.
/// This is critical - both parsers must produce the same logical structure for equivalent queries.
/// </summary>
public sealed class QueryParserEquivalenceTests
{
    private readonly InfixQueryParser _infixParser = new();
    private readonly MongoJsonQueryParser _mongoParser = new();

    [Fact]
    public void Parse_SimpleEquality_ProducesEquivalentAST()
    {
        var infixResult = this._infixParser.Parse("content:kubernetes");
        var mongoResult = this._mongoParser.Parse("{\"content\": \"kubernetes\"}");

        AssertNodesEquivalent(infixResult, mongoResult);
    }

    [Fact]
    public void Parse_NotEqual_ProducesEquivalentAST()
    {
        var infixResult = this._infixParser.Parse("mimeType!=image/png");
        var mongoResult = this._mongoParser.Parse("{\"mimeType\": {\"$ne\": \"image/png\"}}");

        AssertNodesEquivalent(infixResult, mongoResult);
    }

    [Fact]
    public void Parse_GreaterThanOrEqual_ProducesEquivalentAST()
    {
        var infixResult = this._infixParser.Parse("createdAt>=2024-01-01");
        var mongoResult = this._mongoParser.Parse("{\"createdAt\": {\"$gte\": \"2024-01-01\"}}");

        AssertNodesEquivalent(infixResult, mongoResult);
    }

    [Fact]
    public void Parse_LessThan_ProducesEquivalentAST()
    {
        var infixResult = this._infixParser.Parse("createdAt<2024-02-01");
        var mongoResult = this._mongoParser.Parse("{\"createdAt\": {\"$lt\": \"2024-02-01\"}}");

        AssertNodesEquivalent(infixResult, mongoResult);
    }

    [Fact]
    public void Parse_Contains_ProducesEquivalentAST()
    {
        var infixResult = this._infixParser.Parse("content:~\"machine learning\"");
        var mongoResult = this._mongoParser.Parse("{\"content\": {\"$regex\": \"machine learning\"}}");

        AssertNodesEquivalent(infixResult, mongoResult);
    }

    [Fact]
    public void Parse_ArrayIn_ProducesEquivalentAST()
    {
        var infixResult = this._infixParser.Parse("tags:[AI,ML]");
        var mongoResult = this._mongoParser.Parse("{\"tags\": {\"$in\": [\"AI\", \"ML\"]}}");

        AssertNodesEquivalent(infixResult, mongoResult);
    }

    [Fact]
    public void Parse_SimpleAnd_ProducesEquivalentAST()
    {
        var infixResult = this._infixParser.Parse("kubernetes AND docker");
        var mongoResult = this._mongoParser.Parse("{\"$and\": [{\"$text\": {\"$search\": \"kubernetes\"}}, {\"$text\": {\"$search\": \"docker\"}}]}");

        // Both should be LogicalNode with AND operator and 2 children
        var infixLogical = Assert.IsType<LogicalNode>(infixResult);
        var mongoLogical = Assert.IsType<LogicalNode>(mongoResult);

        Assert.Equal(LogicalOperator.And, infixLogical.Operator);
        Assert.Equal(LogicalOperator.And, mongoLogical.Operator);
        Assert.Equal(2, infixLogical.Children.Length);
        Assert.Equal(2, mongoLogical.Children.Length);
    }

    [Fact]
    public void Parse_SimpleOr_ProducesEquivalentAST()
    {
        var infixResult = this._infixParser.Parse("tags:AI OR tags:ML");
        var mongoResult = this._mongoParser.Parse("{\"$or\": [{\"tags\": \"AI\"}, {\"tags\": \"ML\"}]}");

        AssertNodesEquivalent(infixResult, mongoResult);
    }

    [Fact]
    public void Parse_Not_ProducesEquivalentAST()
    {
        var infixResult = this._infixParser.Parse("NOT mimeType:image/png");
        var mongoResult = this._mongoParser.Parse("{\"$not\": {\"mimeType\": \"image/png\"}}");

        AssertNodesEquivalent(infixResult, mongoResult);
    }

    [Fact]
    public void Parse_ComplexBooleanExpression_ProducesEquivalentAST()
    {
        var infixResult = this._infixParser.Parse("(tags:AI OR tags:ML) AND NOT mimeType:image/png");
        var mongoResult = this._mongoParser.Parse("{\"$and\": [{\"$or\": [{\"tags\": \"AI\"}, {\"tags\": \"ML\"}]}, {\"$not\": {\"mimeType\": \"image/png\"}}]}");

        // Both should be AND nodes with 2 children
        var infixLogical = Assert.IsType<LogicalNode>(infixResult);
        var mongoLogical = Assert.IsType<LogicalNode>(mongoResult);

        Assert.Equal(LogicalOperator.And, infixLogical.Operator);
        Assert.Equal(LogicalOperator.And, mongoLogical.Operator);
        Assert.Equal(2, infixLogical.Children.Length);
        Assert.Equal(2, mongoLogical.Children.Length);

        // First child should be OR
        var infixOr = Assert.IsType<LogicalNode>(infixLogical.Children[0]);
        var mongoOr = Assert.IsType<LogicalNode>(mongoLogical.Children[0]);
        Assert.Equal(LogicalOperator.Or, infixOr.Operator);
        Assert.Equal(LogicalOperator.Or, mongoOr.Operator);

        // Second child should be NOT
        var infixNot = Assert.IsType<LogicalNode>(infixLogical.Children[1]);
        var mongoNot = Assert.IsType<LogicalNode>(mongoLogical.Children[1]);
        Assert.Equal(LogicalOperator.Not, infixNot.Operator);
        Assert.Equal(LogicalOperator.Not, mongoNot.Operator);
    }

    [Fact]
    public void Parse_DateRange_ProducesEquivalentAST()
    {
        var infixResult = this._infixParser.Parse("createdAt>=2024-01-01 AND createdAt<2024-02-01");
        var mongoResult = this._mongoParser.Parse("{\"createdAt\": {\"$gte\": \"2024-01-01\", \"$lt\": \"2024-02-01\"}}");

        // Both should be AND nodes with 2 comparison children
        var infixLogical = Assert.IsType<LogicalNode>(infixResult);
        var mongoLogical = Assert.IsType<LogicalNode>(mongoResult);

        Assert.Equal(LogicalOperator.And, infixLogical.Operator);
        Assert.Equal(LogicalOperator.And, mongoLogical.Operator);
        Assert.Equal(2, infixLogical.Children.Length);
        Assert.Equal(2, mongoLogical.Children.Length);

        // Check operators
        var infixLeft = Assert.IsType<ComparisonNode>(infixLogical.Children[0]);
        var mongoLeft = Assert.IsType<ComparisonNode>(mongoLogical.Children[0]);
        Assert.Equal(ComparisonOperator.GreaterThanOrEqual, infixLeft.Operator);
        Assert.Equal(ComparisonOperator.GreaterThanOrEqual, mongoLeft.Operator);

        var infixRight = Assert.IsType<ComparisonNode>(infixLogical.Children[1]);
        var mongoRight = Assert.IsType<ComparisonNode>(mongoLogical.Children[1]);
        Assert.Equal(ComparisonOperator.LessThan, infixRight.Operator);
        Assert.Equal(ComparisonOperator.LessThan, mongoRight.Operator);
    }

    [Fact]
    public void Parse_MetadataFields_ProducesEquivalentAST()
    {
        var infixResult = this._infixParser.Parse("metadata.author:John");
        var mongoResult = this._mongoParser.Parse("{\"metadata.author\": \"John\"}");

        AssertNodesEquivalent(infixResult, mongoResult);
    }

    private static void AssertNodesEquivalent(QueryNode node1, QueryNode node2)
    {
        // Check type equivalence
        Assert.Equal(node1.GetType(), node2.GetType());

        if (node1 is ComparisonNode comp1 && node2 is ComparisonNode comp2)
        {
            Assert.Equal(comp1.Field!.FieldPath, comp2.Field!.FieldPath);
            Assert.Equal(comp1.Operator, comp2.Operator);

            // Compare values (handling arrays specially)
            if (comp1.Value!.Value is string[] arr1 && comp2.Value!.Value is string[] arr2)
            {
                Assert.Equal(arr1, arr2);
            }
            else
            {
                Assert.Equal(comp1.Value!.Value?.ToString(), comp2.Value!.Value?.ToString());
            }
        }
        else if (node1 is LogicalNode logical1 && node2 is LogicalNode logical2)
        {
            Assert.Equal(logical1.Operator, logical2.Operator);
            Assert.Equal(logical1.Children.Length, logical2.Children.Length);

            // Recursively check children
            for (int i = 0; i < logical1.Children.Length; i++)
            {
                AssertNodesEquivalent(logical1.Children[i], logical2.Children[i]);
            }
        }
        else if (node1 is TextSearchNode text1 && node2 is TextSearchNode text2)
        {
            Assert.Equal(text1.SearchText, text2.SearchText);
            Assert.Equal(text1.Field?.FieldPath, text2.Field?.FieldPath);
        }
    }
}
