// Copyright (c) Microsoft. All rights reserved.

using KernelMemory.Core.Search.Query.Parsers;

namespace KernelMemory.Core.Tests.Search.Query;

/// <summary>
/// Tests for QueryParserFactory auto-detection and parser creation.
/// </summary>
public sealed class QueryParserFactoryTests
{
    [Fact]
    public void DetectFormat_JsonQuery_ReturnsMongoJsonParser()
    {
        // Arrange
        const string query = "{\"content\": \"kubernetes\"}";

        // Act
        var parser = QueryParserFactory.DetectFormat(query);

        // Assert
        Assert.IsType<MongoJsonQueryParser>(parser);
    }

    [Fact]
    public void DetectFormat_JsonQueryWithWhitespace_ReturnsMongoJsonParser()
    {
        // Arrange
        const string query = "  \t\n {\"$text\": {\"$search\": \"test\"}}";

        // Act
        var parser = QueryParserFactory.DetectFormat(query);

        // Assert
        Assert.IsType<MongoJsonQueryParser>(parser);
    }

    [Fact]
    public void DetectFormat_InfixQuery_ReturnsInfixParser()
    {
        // Arrange
        const string query = "kubernetes AND docker";

        // Act
        var parser = QueryParserFactory.DetectFormat(query);

        // Assert
        Assert.IsType<InfixQueryParser>(parser);
    }

    [Fact]
    public void DetectFormat_SimpleText_ReturnsInfixParser()
    {
        // Arrange
        const string query = "kubernetes";

        // Act
        var parser = QueryParserFactory.DetectFormat(query);

        // Assert
        Assert.IsType<InfixQueryParser>(parser);
    }

    [Fact]
    public void DetectFormat_EmptyQuery_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => QueryParserFactory.DetectFormat(""));
        Assert.Throws<ArgumentException>(() => QueryParserFactory.DetectFormat("   "));
        Assert.Throws<ArgumentException>(() => QueryParserFactory.DetectFormat("\t\n"));
    }

    [Fact]
    public void Parse_JsonQuery_ReturnsValidAST()
    {
        // Arrange
        const string query = "{\"content\": \"kubernetes\"}";

        // Act
        var ast = QueryParserFactory.Parse(query);

        // Assert
        Assert.NotNull(ast);
    }

    [Fact]
    public void Parse_InfixQuery_ReturnsValidAST()
    {
        // Arrange
        const string query = "kubernetes AND docker";

        // Act
        var ast = QueryParserFactory.Parse(query);

        // Assert
        Assert.NotNull(ast);
    }

    [Fact]
    public void Parse_InvalidJsonQuery_ThrowsQuerySyntaxException()
    {
        // Arrange
        const string query = "{invalid json}";

        // Act & Assert
        Assert.Throws<QuerySyntaxException>(() => QueryParserFactory.Parse(query));
    }

    [Fact]
    public void Parse_InvalidInfixQuery_ThrowsQuerySyntaxException()
    {
        // Arrange
        const string query = "kubernetes AND docker)";

        // Act & Assert
        Assert.Throws<QuerySyntaxException>(() => QueryParserFactory.Parse(query));
    }
}
