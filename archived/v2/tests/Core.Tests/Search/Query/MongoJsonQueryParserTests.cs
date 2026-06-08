// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Core.Search.Query.Ast;
using KernelMemory.Core.Search.Query.Parsers;

namespace KernelMemory.Core.Tests.Search.Query;

/// <summary>
/// Tests for MongoJsonQueryParser with comprehensive coverage of MongoDB operators.
/// </summary>
public sealed class MongoJsonQueryParserTests
{
    private readonly MongoJsonQueryParser _parser = new();

    [Fact]
    public void Parse_SimpleEquality_ReturnsComparisonNode()
    {
        var result = this._parser.Parse("{\"content\": \"kubernetes\"}");

        var compNode = Assert.IsType<ComparisonNode>(result);
        Assert.Equal("content", compNode.Field!.FieldPath);
        Assert.Equal(ComparisonOperator.Equal, compNode.Operator);
        Assert.Equal("kubernetes", compNode.Value!.AsString());
    }

    [Fact]
    public void Parse_EqOperator_ReturnsComparisonNode()
    {
        var result = this._parser.Parse("{\"tags\": {\"$eq\": \"production\"}}");

        var compNode = Assert.IsType<ComparisonNode>(result);
        Assert.Equal("tags", compNode.Field!.FieldPath);
        Assert.Equal(ComparisonOperator.Equal, compNode.Operator);
        Assert.Equal("production", compNode.Value!.AsString());
    }

    [Fact]
    public void Parse_NeOperator_ReturnsComparisonNode()
    {
        var result = this._parser.Parse("{\"mimeType\": {\"$ne\": \"image/png\"}}");

        var compNode = Assert.IsType<ComparisonNode>(result);
        Assert.Equal("mimetype", compNode.Field!.FieldPath);
        Assert.Equal(ComparisonOperator.NotEqual, compNode.Operator);
        Assert.Equal("image/png", compNode.Value!.AsString());
    }

    [Fact]
    public void Parse_GtOperator_ReturnsComparisonNode()
    {
        var result = this._parser.Parse("{\"createdAt\": {\"$gt\": \"2024-01-01\"}}");

        var compNode = Assert.IsType<ComparisonNode>(result);
        Assert.Equal("createdat", compNode.Field!.FieldPath);
        Assert.Equal(ComparisonOperator.GreaterThan, compNode.Operator);
        Assert.Equal("2024-01-01", compNode.Value!.AsString());
    }

    [Fact]
    public void Parse_GteOperator_ReturnsComparisonNode()
    {
        var result = this._parser.Parse("{\"createdAt\": {\"$gte\": \"2024-01-01\"}}");

        var compNode = Assert.IsType<ComparisonNode>(result);
        Assert.Equal("createdat", compNode.Field!.FieldPath);
        Assert.Equal(ComparisonOperator.GreaterThanOrEqual, compNode.Operator);
        Assert.Equal("2024-01-01", compNode.Value!.AsString());
    }

    [Fact]
    public void Parse_LtOperator_ReturnsComparisonNode()
    {
        var result = this._parser.Parse("{\"createdAt\": {\"$lt\": \"2024-02-01\"}}");

        var compNode = Assert.IsType<ComparisonNode>(result);
        Assert.Equal("createdat", compNode.Field!.FieldPath);
        Assert.Equal(ComparisonOperator.LessThan, compNode.Operator);
        Assert.Equal("2024-02-01", compNode.Value!.AsString());
    }

    [Fact]
    public void Parse_LteOperator_ReturnsComparisonNode()
    {
        var result = this._parser.Parse("{\"createdAt\": {\"$lte\": \"2024-02-01\"}}");

        var compNode = Assert.IsType<ComparisonNode>(result);
        Assert.Equal("createdat", compNode.Field!.FieldPath);
        Assert.Equal(ComparisonOperator.LessThanOrEqual, compNode.Operator);
        Assert.Equal("2024-02-01", compNode.Value!.AsString());
    }

    [Fact]
    public void Parse_InOperator_ReturnsComparisonNode()
    {
        var result = this._parser.Parse("{\"tags\": {\"$in\": [\"AI\", \"ML\", \"research\"]}}");

        var compNode = Assert.IsType<ComparisonNode>(result);
        Assert.Equal("tags", compNode.Field!.FieldPath);
        Assert.Equal(ComparisonOperator.In, compNode.Operator);
        var array = compNode.Value!.AsStringArray();
        Assert.Equal(3, array.Length);
        Assert.Contains("AI", array);
        Assert.Contains("ML", array);
        Assert.Contains("research", array);
    }

    [Fact]
    public void Parse_NinOperator_ReturnsComparisonNode()
    {
        var result = this._parser.Parse("{\"tags\": {\"$nin\": [\"deprecated\", \"archived\"]}}");

        var compNode = Assert.IsType<ComparisonNode>(result);
        Assert.Equal("tags", compNode.Field!.FieldPath);
        Assert.Equal(ComparisonOperator.NotIn, compNode.Operator);
        var array = compNode.Value!.AsStringArray();
        Assert.Equal(2, array.Length);
        Assert.Contains("deprecated", array);
        Assert.Contains("archived", array);
    }

    [Fact]
    public void Parse_RegexOperator_ReturnsComparisonNode()
    {
        var result = this._parser.Parse("{\"content\": {\"$regex\": \"kubernetes\"}}");

        var compNode = Assert.IsType<ComparisonNode>(result);
        Assert.Equal("content", compNode.Field!.FieldPath);
        Assert.Equal(ComparisonOperator.Contains, compNode.Operator);
        Assert.Equal("kubernetes", compNode.Value!.AsString());
    }

    [Fact]
    public void Parse_ExistsOperator_ReturnsComparisonNode()
    {
        var result = this._parser.Parse("{\"metadata.category\": {\"$exists\": true}}");

        var compNode = Assert.IsType<ComparisonNode>(result);
        Assert.Equal("metadata.category", compNode.Field!.FieldPath);
        Assert.Equal(ComparisonOperator.Exists, compNode.Operator);
        Assert.True((bool)compNode.Value!.Value);
    }

    [Fact]
    public void Parse_ExistsFalse_ReturnsLogicalNotNode()
    {
        var result = this._parser.Parse("{\"metadata.category\": {\"$exists\": false}}");

        var notNode = Assert.IsType<LogicalNode>(result);
        Assert.Equal(LogicalOperator.Not, notNode.Operator);
        Assert.Single(notNode.Children);

        var compNode = Assert.IsType<ComparisonNode>(notNode.Children[0]);
        Assert.Equal("metadata.category", compNode.Field!.FieldPath);
        Assert.Equal(ComparisonOperator.Exists, compNode.Operator);
    }

    [Fact]
    public void Parse_TextOperator_ReturnsTextSearchNode()
    {
        var result = this._parser.Parse("{\"$text\": {\"$search\": \"kubernetes\"}}");

        var textNode = Assert.IsType<TextSearchNode>(result);
        Assert.Equal("kubernetes", textNode.SearchText);
        Assert.Null(textNode.Field);
    }

    [Fact]
    public void Parse_AndOperator_ReturnsLogicalNode()
    {
        var result = this._parser.Parse("{\"$and\": [{\"tags\": \"AI\"}, {\"createdAt\": {\"$gte\": \"2024-01-01\"}}]}");

        var logicalNode = Assert.IsType<LogicalNode>(result);
        Assert.Equal(LogicalOperator.And, logicalNode.Operator);
        Assert.Equal(2, logicalNode.Children.Length);

        var left = Assert.IsType<ComparisonNode>(logicalNode.Children[0]);
        Assert.Equal("tags", left.Field.FieldPath);

        var right = Assert.IsType<ComparisonNode>(logicalNode.Children[1]);
        Assert.Equal("createdat", right.Field.FieldPath);
    }

    [Fact]
    public void Parse_OrOperator_ReturnsLogicalNode()
    {
        var result = this._parser.Parse("{\"$or\": [{\"tags\": \"AI\"}, {\"tags\": \"ML\"}]}");

        var logicalNode = Assert.IsType<LogicalNode>(result);
        Assert.Equal(LogicalOperator.Or, logicalNode.Operator);
        Assert.Equal(2, logicalNode.Children.Length);
    }

    [Fact]
    public void Parse_NotOperator_ReturnsLogicalNode()
    {
        var result = this._parser.Parse("{\"$not\": {\"mimeType\": \"image/png\"}}");

        var logicalNode = Assert.IsType<LogicalNode>(result);
        Assert.Equal(LogicalOperator.Not, logicalNode.Operator);
        Assert.Single(logicalNode.Children);

        var compNode = Assert.IsType<ComparisonNode>(logicalNode.Children[0]);
        Assert.Equal("mimetype", compNode.Field!.FieldPath);
    }

    [Fact]
    public void Parse_NorOperator_ReturnsLogicalNode()
    {
        var result = this._parser.Parse("{\"$nor\": [{\"tags\": \"deprecated\"}, {\"tags\": \"archived\"}]}");

        var logicalNode = Assert.IsType<LogicalNode>(result);
        Assert.Equal(LogicalOperator.Nor, logicalNode.Operator);
        Assert.Equal(2, logicalNode.Children.Length);
    }

    [Fact]
    public void Parse_ComplexNestedQuery_ReturnsLogicalNode()
    {
        const string query = @"{
            ""$and"": [
                {""$or"": [{""tags"": ""AI""}, {""tags"": ""ML""}]},
                {""$not"": {""mimeType"": ""image/png""}}
            ]
        }";

        var result = this._parser.Parse(query);

        var rootAnd = Assert.IsType<LogicalNode>(result);
        Assert.Equal(LogicalOperator.And, rootAnd.Operator);
        Assert.Equal(2, rootAnd.Children.Length);

        // First child: OR node
        var orNode = Assert.IsType<LogicalNode>(rootAnd.Children[0]);
        Assert.Equal(LogicalOperator.Or, orNode.Operator);

        // Second child: NOT node
        var notNode = Assert.IsType<LogicalNode>(rootAnd.Children[1]);
        Assert.Equal(LogicalOperator.Not, notNode.Operator);
    }

    [Fact]
    public void Parse_MetadataField_ReturnsComparisonNode()
    {
        var result = this._parser.Parse("{\"metadata.author\": \"John\"}");

        var compNode = Assert.IsType<ComparisonNode>(result);
        Assert.Equal("metadata.author", compNode.Field!.FieldPath);
        Assert.Equal(ComparisonOperator.Equal, compNode.Operator);
        Assert.Equal("John", compNode.Value!.AsString());
    }

    [Fact]
    public void Parse_MultipleFieldsAtRoot_ReturnsLogicalAndNode()
    {
        var result = this._parser.Parse("{\"metadata.author\": \"John\", \"metadata.department\": \"AI\"}");

        var logicalNode = Assert.IsType<LogicalNode>(result);
        Assert.Equal(LogicalOperator.And, logicalNode.Operator);
        Assert.Equal(2, logicalNode.Children.Length);

        var left = Assert.IsType<ComparisonNode>(logicalNode.Children[0]);
        Assert.Equal("metadata.author", left.Field.FieldPath);

        var right = Assert.IsType<ComparisonNode>(logicalNode.Children[1]);
        Assert.Equal("metadata.department", right.Field.FieldPath);
    }

    [Fact]
    public void Parse_DateRangeQuery_ReturnsLogicalAndNode()
    {
        var result = this._parser.Parse("{\"createdAt\": {\"$gte\": \"2024-01-01\", \"$lt\": \"2024-02-01\"}}");

        var logicalNode = Assert.IsType<LogicalNode>(result);
        Assert.Equal(LogicalOperator.And, logicalNode.Operator);
        Assert.Equal(2, logicalNode.Children.Length);

        var left = Assert.IsType<ComparisonNode>(logicalNode.Children[0]);
        Assert.Equal(ComparisonOperator.GreaterThanOrEqual, left.Operator);

        var right = Assert.IsType<ComparisonNode>(logicalNode.Children[1]);
        Assert.Equal(ComparisonOperator.LessThan, right.Operator);
    }

    [Fact]
    public void Parse_EmptyQuery_ThrowsException()
    {
        Assert.Throws<QuerySyntaxException>(() => this._parser.Parse(""));
        Assert.Throws<QuerySyntaxException>(() => this._parser.Parse("   "));
    }

    [Fact]
    public void Parse_InvalidJson_ThrowsException()
    {
        Assert.Throws<QuerySyntaxException>(() => this._parser.Parse("{invalid json}"));
        Assert.Throws<QuerySyntaxException>(() => this._parser.Parse("{\"field\": unclosed"));
    }

    [Fact]
    public void Parse_UnknownOperator_ThrowsException()
    {
        Assert.Throws<QuerySyntaxException>(() => this._parser.Parse("{\"field\": {\"$unknown\": \"value\"}}"));
    }

    [Fact]
    public void Parse_EmptyObject_ThrowsException()
    {
        Assert.Throws<QuerySyntaxException>(() => this._parser.Parse("{}"));
    }

    [Fact]
    public void Validate_ValidQuery_ReturnsTrue()
    {
        Assert.True(this._parser.Validate("{\"content\": \"kubernetes\"}"));
        Assert.True(this._parser.Validate("{\"$and\": [{\"tags\": \"AI\"}, {\"tags\": \"ML\"}]}"));
    }

    [Fact]
    public void Validate_InvalidQuery_ReturnsFalse()
    {
        Assert.False(this._parser.Validate(""));
        Assert.False(this._parser.Validate("{invalid}"));
        Assert.False(this._parser.Validate("{}"));
    }
}
