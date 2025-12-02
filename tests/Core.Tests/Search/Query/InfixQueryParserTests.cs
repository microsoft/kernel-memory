// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Core.Search.Query.Ast;
using KernelMemory.Core.Search.Query.Parsers;

namespace KernelMemory.Core.Tests.Search.Query;

/// <summary>
/// Tests for InfixQueryParser with comprehensive coverage of all query syntax.
/// </summary>
public sealed class InfixQueryParserTests
{
    private readonly InfixQueryParser _parser = new();

    [Fact]
    public void Parse_SimpleTextSearch_ReturnsTextSearchNode()
    {
        // Simple query without field prefix should search all FTS fields
        var result = this._parser.Parse("kubernetes");

        Assert.NotNull(result);
        var textNode = Assert.IsType<TextSearchNode>(result);
        Assert.Equal("kubernetes", textNode.SearchText);
        Assert.Null(textNode.Field);
    }

    [Fact]
    public void Parse_QuotedTextSearch_ReturnsTextSearchNodeWithSpaces()
    {
        var result = this._parser.Parse("\"machine learning\"");

        var textNode = Assert.IsType<TextSearchNode>(result);
        Assert.Equal("machine learning", textNode.SearchText);
    }

    [Fact]
    public void Parse_FieldEquality_ReturnsComparisonNode()
    {
        var result = this._parser.Parse("content:kubernetes");

        var compNode = Assert.IsType<ComparisonNode>(result);
        Assert.Equal("content", compNode.Field!.FieldPath);
        Assert.Equal(ComparisonOperator.Equal, compNode.Operator);
        Assert.Equal("kubernetes", compNode.Value!.AsString());
    }

    [Fact]
    public void Parse_FieldEqualityWithDoubleEquals_ReturnsComparisonNode()
    {
        var result = this._parser.Parse("tags==production");

        var compNode = Assert.IsType<ComparisonNode>(result);
        Assert.Equal("tags", compNode.Field!.FieldPath);
        Assert.Equal(ComparisonOperator.Equal, compNode.Operator);
        Assert.Equal("production", compNode.Value!.AsString());
    }

    [Fact]
    public void Parse_FieldNotEqual_ReturnsComparisonNode()
    {
        var result = this._parser.Parse("mimeType!=image/png");

        var compNode = Assert.IsType<ComparisonNode>(result);
        Assert.Equal("mimetype", compNode.Field!.FieldPath);
        Assert.Equal(ComparisonOperator.NotEqual, compNode.Operator);
        Assert.Equal("image/png", compNode.Value!.AsString());
    }

    [Fact]
    public void Parse_FieldGreaterThanOrEqual_ReturnsComparisonNode()
    {
        var result = this._parser.Parse("createdAt>=2024-01-01");

        var compNode = Assert.IsType<ComparisonNode>(result);
        Assert.Equal("createdat", compNode.Field!.FieldPath);
        Assert.Equal(ComparisonOperator.GreaterThanOrEqual, compNode.Operator);
        Assert.Equal("2024-01-01", compNode.Value!.AsString());
    }

    [Fact]
    public void Parse_FieldLessThan_ReturnsComparisonNode()
    {
        var result = this._parser.Parse("createdAt<2024-02-01");

        var compNode = Assert.IsType<ComparisonNode>(result);
        Assert.Equal("createdat", compNode.Field!.FieldPath);
        Assert.Equal(ComparisonOperator.LessThan, compNode.Operator);
        Assert.Equal("2024-02-01", compNode.Value!.AsString());
    }

    [Fact]
    public void Parse_FieldContains_ReturnsComparisonNode()
    {
        var result = this._parser.Parse("content:~\"machine learning\"");

        var compNode = Assert.IsType<ComparisonNode>(result);
        Assert.Equal("content", compNode.Field!.FieldPath);
        Assert.Equal(ComparisonOperator.Contains, compNode.Operator);
        Assert.Equal("machine learning", compNode.Value!.AsString());
    }

    [Fact]
    public void Parse_ArrayIn_ReturnsComparisonNode()
    {
        var result = this._parser.Parse("tags:[AI,ML,research]");

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
    public void Parse_MetadataField_ReturnsComparisonNode()
    {
        var result = this._parser.Parse("metadata.author:John");

        var compNode = Assert.IsType<ComparisonNode>(result);
        Assert.Equal("metadata.author", compNode.Field!.FieldPath);
        Assert.Equal(ComparisonOperator.Equal, compNode.Operator);
        Assert.Equal("John", compNode.Value!.AsString());
    }

    [Fact]
    public void Parse_SimpleAnd_ReturnsLogicalNode()
    {
        var result = this._parser.Parse("kubernetes AND docker");

        var logicalNode = Assert.IsType<LogicalNode>(result);
        Assert.Equal(LogicalOperator.And, logicalNode.Operator);
        Assert.Equal(2, logicalNode.Children.Length);

        var left = Assert.IsType<TextSearchNode>(logicalNode.Children[0]);
        Assert.Equal("kubernetes", left.SearchText);

        var right = Assert.IsType<TextSearchNode>(logicalNode.Children[1]);
        Assert.Equal("docker", right.SearchText);
    }

    [Fact]
    public void Parse_SimpleOr_ReturnsLogicalNode()
    {
        var result = this._parser.Parse("kubernetes OR docker");

        var logicalNode = Assert.IsType<LogicalNode>(result);
        Assert.Equal(LogicalOperator.Or, logicalNode.Operator);
        Assert.Equal(2, logicalNode.Children.Length);
    }

    [Fact]
    public void Parse_Not_ReturnsLogicalNode()
    {
        var result = this._parser.Parse("NOT kubernetes");

        var logicalNode = Assert.IsType<LogicalNode>(result);
        Assert.Equal(LogicalOperator.Not, logicalNode.Operator);
        Assert.Single(logicalNode.Children);

        var child = Assert.IsType<TextSearchNode>(logicalNode.Children[0]);
        Assert.Equal("kubernetes", child.SearchText);
    }

    [Fact]
    public void Parse_ComplexBooleanWithParentheses_ReturnsLogicalNode()
    {
        var result = this._parser.Parse("(tags:AI OR tags:ML) AND NOT mimeType:image/png");

        var rootAnd = Assert.IsType<LogicalNode>(result);
        Assert.Equal(LogicalOperator.And, rootAnd.Operator);
        Assert.Equal(2, rootAnd.Children.Length);

        // First child: (tags:AI OR tags:ML)
        var orNode = Assert.IsType<LogicalNode>(rootAnd.Children[0]);
        Assert.Equal(LogicalOperator.Or, orNode.Operator);
        Assert.Equal(2, orNode.Children.Length);

        // Second child: NOT mimeType:image/png
        var notNode = Assert.IsType<LogicalNode>(rootAnd.Children[1]);
        Assert.Equal(LogicalOperator.Not, notNode.Operator);
        Assert.Single(notNode.Children);
    }

    [Fact]
    public void Parse_DateRangeQuery_ReturnsLogicalNode()
    {
        var result = this._parser.Parse("createdAt>=2024-01-01 AND createdAt<2024-02-01");

        var logicalNode = Assert.IsType<LogicalNode>(result);
        Assert.Equal(LogicalOperator.And, logicalNode.Operator);
        Assert.Equal(2, logicalNode.Children.Length);

        var left = Assert.IsType<ComparisonNode>(logicalNode.Children[0]);
        Assert.Equal(ComparisonOperator.GreaterThanOrEqual, left.Operator);

        var right = Assert.IsType<ComparisonNode>(logicalNode.Children[1]);
        Assert.Equal(ComparisonOperator.LessThan, right.Operator);
    }

    [Fact]
    public void Parse_MixedFieldAndDefaultSearch_ReturnsLogicalNode()
    {
        var result = this._parser.Parse("kubernetes AND tags:production");

        var logicalNode = Assert.IsType<LogicalNode>(result);
        Assert.Equal(LogicalOperator.And, logicalNode.Operator);
        Assert.Equal(2, logicalNode.Children.Length);

        // First child: default search
        var textNode = Assert.IsType<TextSearchNode>(logicalNode.Children[0]);
        Assert.Equal("kubernetes", textNode.SearchText);

        // Second child: field search
        var compNode = Assert.IsType<ComparisonNode>(logicalNode.Children[1]);
        Assert.Equal("tags", compNode.Field!.FieldPath);
    }

    [Fact]
    public void Parse_CaseInsensitiveBooleanOperators_ReturnsLogicalNode()
    {
        // Test lowercase
        var result1 = this._parser.Parse("kubernetes and docker");
        var logical1 = Assert.IsType<LogicalNode>(result1);
        Assert.Equal(LogicalOperator.And, logical1.Operator);

        // Test mixed case
        var result2 = this._parser.Parse("kubernetes Or docker");
        var logical2 = Assert.IsType<LogicalNode>(result2);
        Assert.Equal(LogicalOperator.Or, logical2.Operator);

        // Test uppercase
        var result3 = this._parser.Parse("NOT kubernetes");
        var logical3 = Assert.IsType<LogicalNode>(result3);
        Assert.Equal(LogicalOperator.Not, logical3.Operator);
    }

    [Fact]
    public void Parse_NestedParentheses_ReturnsLogicalNode()
    {
        var result = this._parser.Parse("((tags:AI OR tags:ML) AND content:kubernetes) OR tags:docker");

        var rootOr = Assert.IsType<LogicalNode>(result);
        Assert.Equal(LogicalOperator.Or, rootOr.Operator);
        Assert.Equal(2, rootOr.Children.Length);

        // First child should be nested AND
        var andNode = Assert.IsType<LogicalNode>(rootOr.Children[0]);
        Assert.Equal(LogicalOperator.And, andNode.Operator);
    }

    [Fact]
    public void Parse_EmptyQuery_ThrowsException()
    {
        Assert.Throws<QuerySyntaxException>(() => this._parser.Parse(""));
        Assert.Throws<QuerySyntaxException>(() => this._parser.Parse("   "));
    }

    [Fact]
    public void Parse_UnmatchedParenthesis_ThrowsException()
    {
        Assert.Throws<QuerySyntaxException>(() => this._parser.Parse("(kubernetes AND docker"));
        Assert.Throws<QuerySyntaxException>(() => this._parser.Parse("kubernetes AND docker)"));
    }

    [Fact]
    public void Validate_ValidQuery_ReturnsTrue()
    {
        Assert.True(this._parser.Validate("kubernetes"));
        Assert.True(this._parser.Validate("content:kubernetes AND tags:AI"));
        Assert.True(this._parser.Validate("(A OR B) AND NOT C"));
    }

    [Fact]
    public void Validate_InvalidQuery_ReturnsFalse()
    {
        Assert.False(this._parser.Validate(""));
        Assert.False(this._parser.Validate("(unmatched"));
    }

    [Fact]
    public void Parse_MultipleMetadataFields_ReturnsLogicalNode()
    {
        var result = this._parser.Parse("metadata.author:John AND metadata.department:AI");

        var logicalNode = Assert.IsType<LogicalNode>(result);
        Assert.Equal(LogicalOperator.And, logicalNode.Operator);
        Assert.Equal(2, logicalNode.Children.Length);

        var left = Assert.IsType<ComparisonNode>(logicalNode.Children[0]);
        Assert.Equal("metadata.author", left.Field.FieldPath);

        var right = Assert.IsType<ComparisonNode>(logicalNode.Children[1]);
        Assert.Equal("metadata.department", right.Field.FieldPath);
    }

    [Fact]
    public void Parse_OperatorPrecedence_NotHigherThanAnd()
    {
        // NOT should have higher precedence than AND
        var result = this._parser.Parse("kubernetes AND NOT docker");

        var andNode = Assert.IsType<LogicalNode>(result);
        Assert.Equal(LogicalOperator.And, andNode.Operator);
        Assert.Equal(2, andNode.Children.Length);

        // First child: kubernetes (text search)
        Assert.IsType<TextSearchNode>(andNode.Children[0]);

        // Second child: NOT docker (logical NOT)
        var notNode = Assert.IsType<LogicalNode>(andNode.Children[1]);
        Assert.Equal(LogicalOperator.Not, notNode.Operator);
    }

    [Fact]
    public void Parse_OperatorPrecedence_AndHigherThanOr()
    {
        // AND should have higher precedence than OR
        var result = this._parser.Parse("A OR B AND C");

        var orNode = Assert.IsType<LogicalNode>(result);
        Assert.Equal(LogicalOperator.Or, orNode.Operator);
        Assert.Equal(2, orNode.Children.Length);

        // First child: A
        var firstText = Assert.IsType<TextSearchNode>(orNode.Children[0]);
        Assert.Equal("A", firstText.SearchText);

        // Second child: B AND C
        var andNode = Assert.IsType<LogicalNode>(orNode.Children[1]);
        Assert.Equal(LogicalOperator.And, andNode.Operator);
    }

    [Fact]
    public void Parse_QuotedValueWithSpecialCharacters_ReturnsComparisonNode()
    {
        var result = this._parser.Parse("content:\"test:value with:colons\"");

        var compNode = Assert.IsType<ComparisonNode>(result);
        Assert.Equal("content", compNode.Field!.FieldPath);
        Assert.Equal("test:value with:colons", compNode.Value!.AsString());
    }
}
