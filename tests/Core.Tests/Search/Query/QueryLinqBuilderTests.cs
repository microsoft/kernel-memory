// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Core.Search.Query;
using KernelMemory.Core.Search.Query.Ast;
using KernelMemory.Core.Storage.Entities;

namespace KernelMemory.Core.Tests.Search.Query;

/// <summary>
/// Tests for QueryLinqBuilder verifying AST to LINQ transformation.
/// Tests NoSQL semantics, case-insensitive matching, metadata dot notation, and type conversions.
/// </summary>
public sealed class QueryLinqBuilderTests
{
    private readonly QueryLinqBuilder _builder = new(typeof(ContentRecord));

    [Fact]
    public void Build_SimpleFieldEquality_GeneratesCorrectLinq()
    {
        var query = new ComparisonNode
        {
            Field = new FieldNode { FieldPath = "content" },
            Operator = ComparisonOperator.Equal,
            Value = new LiteralNode { Value = "kubernetes" }
        };

        var expr = this._builder.Build<ContentRecord>(query);

        // Test with sample data
        var records = new[]
        {
            new ContentRecord { Id = "1", Content = "kubernetes guide" },
            new ContentRecord { Id = "2", Content = "docker tutorial" }
        };

        var results = records.AsQueryable().Where(expr).ToArray();

        Assert.Single(results);
        Assert.Equal("1", results[0].Id);
    }

    [Fact]
    public void Build_CaseInsensitiveMatch_WorksCorrectly()
    {
        var query = new ComparisonNode
        {
            Field = new FieldNode { FieldPath = "content" },
            Operator = ComparisonOperator.Equal,
            Value = new LiteralNode { Value = "KUBERNETES" }
        };

        var expr = this._builder.Build<ContentRecord>(query);

        var records = new[]
        {
            new ContentRecord { Id = "1", Content = "kubernetes guide" },
            new ContentRecord { Id = "2", Content = "Kubernetes Tutorial" }
        };

        var results = records.AsQueryable().Where(expr).ToArray();

        // Both should match due to case-insensitive comparison
        Assert.Equal(2, results.Length);
    }

    [Fact]
    public void Build_NotEqualOperator_GeneratesCorrectLinq()
    {
        var query = new ComparisonNode
        {
            Field = new FieldNode { FieldPath = "mimeType" },
            Operator = ComparisonOperator.NotEqual,
            Value = new LiteralNode { Value = "image/png" }
        };

        var expr = this._builder.Build<ContentRecord>(query);

        var records = new[]
        {
            new ContentRecord { Id = "1", Content = "test", MimeType = "text/plain" },
            new ContentRecord { Id = "2", Content = "test", MimeType = "image/png" },
            new ContentRecord { Id = "3", Content = "test", MimeType = "application/pdf" }
        };

        var results = records.AsQueryable().Where(expr).ToArray();

        Assert.Equal(2, results.Length);
        Assert.DoesNotContain(results, r => r.Id == "2");
    }

    [Fact]
    public void Build_ContainsOperator_GeneratesCorrectLinq()
    {
        var query = new ComparisonNode
        {
            Field = new FieldNode { FieldPath = "content" },
            Operator = ComparisonOperator.Contains,
            Value = new LiteralNode { Value = "machine" }
        };

        var expr = this._builder.Build<ContentRecord>(query);

        var records = new[]
        {
            new ContentRecord { Id = "1", Content = "machine learning guide" },
            new ContentRecord { Id = "2", Content = "docker tutorial" },
            new ContentRecord { Id = "3", Content = "The Machine operates well" }
        };

        var results = records.AsQueryable().Where(expr).ToArray();

        Assert.Equal(2, results.Length);
        Assert.Contains(results, r => r.Id == "1");
        Assert.Contains(results, r => r.Id == "3");
    }

    [Fact]
    public void Build_MetadataFieldPositiveMatch_NoSqlSemantics()
    {
        var query = new ComparisonNode
        {
            Field = new FieldNode { FieldPath = "metadata.author" },
            Operator = ComparisonOperator.Equal,
            Value = new LiteralNode { Value = "John" }
        };

        var expr = this._builder.Build<ContentRecord>(query);

        var records = new[]
        {
            new ContentRecord { Id = "1", Content = "test", Metadata = new Dictionary<string, string> { ["author"] = "John" } },
            new ContentRecord { Id = "2", Content = "test", Metadata = new Dictionary<string, string> { ["author"] = "Jane" } },
            new ContentRecord { Id = "3", Content = "test", Metadata = new Dictionary<string, string>() }, // No author field
            new ContentRecord { Id = "4", Content = "test", Metadata = null! }
        };

        var results = records.AsQueryable().Where(expr).ToArray();

        // Only record 1 should match (has the field AND matches value)
        Assert.Single(results);
        Assert.Equal("1", results[0].Id);
    }

    [Fact]
    public void Build_MetadataFieldNegativeMatch_NoSqlSemantics()
    {
        var query = new ComparisonNode
        {
            Field = new FieldNode { FieldPath = "metadata.author" },
            Operator = ComparisonOperator.NotEqual,
            Value = new LiteralNode { Value = "John" }
        };

        var expr = this._builder.Build<ContentRecord>(query);

        var records = new[]
        {
            new ContentRecord { Id = "1", Content = "test", Metadata = new Dictionary<string, string> { ["author"] = "John" } },
            new ContentRecord { Id = "2", Content = "test", Metadata = new Dictionary<string, string> { ["author"] = "Jane" } },
            new ContentRecord { Id = "3", Content = "test", Metadata = new Dictionary<string, string>() }, // No author field
            new ContentRecord { Id = "4", Content = "test", Metadata = null! }
        };

        var results = records.AsQueryable().Where(expr).ToArray();

        // Records 2, 3, 4 should match (don't have the field OR have different value)
        Assert.Equal(3, results.Length);
        Assert.Contains(results, r => r.Id == "2");
        Assert.Contains(results, r => r.Id == "3");
        Assert.Contains(results, r => r.Id == "4");
    }

    [Fact]
    public void Build_TagsArrayContains_GeneratesCorrectLinq()
    {
        var query = new ComparisonNode
        {
            Field = new FieldNode { FieldPath = "tags" },
            Operator = ComparisonOperator.In,
            Value = new LiteralNode { Value = new[] { "AI", "ML" } }
        };

        var expr = this._builder.Build<ContentRecord>(query);

        var records = new[]
        {
            new ContentRecord { Id = "1", Content = "test", Tags = new[] { "AI", "research" } },
            new ContentRecord { Id = "2", Content = "test", Tags = new[] { "docker", "kubernetes" } },
            new ContentRecord { Id = "3", Content = "test", Tags = new[] { "ML", "python" } },
            new ContentRecord { Id = "4", Content = "test", Tags = null! }
        };

        var results = records.AsQueryable().Where(expr).ToArray();

        // Records 1 and 3 should match (have at least one of the tags)
        Assert.Equal(2, results.Length);
        Assert.Contains(results, r => r.Id == "1");
        Assert.Contains(results, r => r.Id == "3");
    }

    [Fact]
    public void Build_LogicalAnd_GeneratesCorrectLinq()
    {
        var query = new LogicalNode
        {
            Operator = LogicalOperator.And,
            Children = new QueryNode[]
            {
                new ComparisonNode
                {
                    Field = new FieldNode { FieldPath = "content" },
                    Operator = ComparisonOperator.Contains,
                    Value = new LiteralNode { Value = "kubernetes" }
                },
                new ComparisonNode
                {
                    Field = new FieldNode { FieldPath = "tags" },
                    Operator = ComparisonOperator.In,
                    Value = new LiteralNode { Value = new[] { "production" } }
                }
            }
        };

        var expr = this._builder.Build<ContentRecord>(query);

        var records = new[]
        {
            new ContentRecord { Id = "1", Content = "kubernetes guide", Tags = new[] { "production" } },
            new ContentRecord { Id = "2", Content = "kubernetes tutorial", Tags = new[] { "dev" } },
            new ContentRecord { Id = "3", Content = "docker guide", Tags = new[] { "production" } }
        };

        var results = records.AsQueryable().Where(expr).ToArray();

        // Only record 1 matches both conditions
        Assert.Single(results);
        Assert.Equal("1", results[0].Id);
    }

    [Fact]
    public void Build_LogicalOr_GeneratesCorrectLinq()
    {
        var query = new LogicalNode
        {
            Operator = LogicalOperator.Or,
            Children = new QueryNode[]
            {
                new ComparisonNode
                {
                    Field = new FieldNode { FieldPath = "tags" },
                    Operator = ComparisonOperator.In,
                    Value = new LiteralNode { Value = new[] { "AI" } }
                },
                new ComparisonNode
                {
                    Field = new FieldNode { FieldPath = "tags" },
                    Operator = ComparisonOperator.In,
                    Value = new LiteralNode { Value = new[] { "ML" } }
                }
            }
        };

        var expr = this._builder.Build<ContentRecord>(query);

        var records = new[]
        {
            new ContentRecord { Id = "1", Content = "test", Tags = new[] { "AI" } },
            new ContentRecord { Id = "2", Content = "test", Tags = new[] { "ML" } },
            new ContentRecord { Id = "3", Content = "test", Tags = new[] { "docker" } }
        };

        var results = records.AsQueryable().Where(expr).ToArray();

        Assert.Equal(2, results.Length);
        Assert.Contains(results, r => r.Id == "1");
        Assert.Contains(results, r => r.Id == "2");
    }

    [Fact]
    public void Build_LogicalNot_GeneratesCorrectLinq()
    {
        var query = new LogicalNode
        {
            Operator = LogicalOperator.Not,
            Children = new QueryNode[]
            {
                new ComparisonNode
                {
                    Field = new FieldNode { FieldPath = "mimeType" },
                    Operator = ComparisonOperator.Equal,
                    Value = new LiteralNode { Value = "image/png" }
                }
            }
        };

        var expr = this._builder.Build<ContentRecord>(query);

        var records = new[]
        {
            new ContentRecord { Id = "1", Content = "test", MimeType = "text/plain" },
            new ContentRecord { Id = "2", Content = "test", MimeType = "image/png" },
            new ContentRecord { Id = "3", Content = "test", MimeType = "application/pdf" }
        };

        var results = records.AsQueryable().Where(expr).ToArray();

        Assert.Equal(2, results.Length);
        Assert.DoesNotContain(results, r => r.Id == "2");
    }

    [Fact]
    public void Build_TextSearchDefaultField_SearchesAllFtsFields()
    {
        var query = new TextSearchNode
        {
            SearchText = "kubernetes",
            Field = null! // Default field behavior
        };

        var expr = this._builder.Build<ContentRecord>(query);

        var records = new[]
        {
            new ContentRecord { Id = "1", Title = "Kubernetes Guide", Content = "test", Description = null! },
            new ContentRecord { Id = "2", Title = "Docker", Content = "test", Description = "About Kubernetes" },
            new ContentRecord { Id = "3", Title = "Docker", Content = "Working with kubernetes clusters", Description = null! },
            new ContentRecord { Id = "4", Title = "Docker", Content = "test", Description = "test" }
        };

        var results = records.AsQueryable().Where(expr).ToArray();

        // Records 1, 2, 3 should match (kubernetes in title, description, or content)
        Assert.Equal(3, results.Length);
        Assert.Contains(results, r => r.Id == "1");
        Assert.Contains(results, r => r.Id == "2");
        Assert.Contains(results, r => r.Id == "3");
    }

    [Fact]
    public void Build_ExistsOperator_ChecksFieldPresence()
    {
        var query = new ComparisonNode
        {
            Field = new FieldNode { FieldPath = "metadata.category" },
            Operator = ComparisonOperator.Exists,
            Value = new LiteralNode { Value = true }
        };

        var expr = this._builder.Build<ContentRecord>(query);

        var records = new[]
        {
            new ContentRecord { Id = "1", Content = "test", Metadata = new Dictionary<string, string> { ["category"] = "tech" } },
            new ContentRecord { Id = "2", Content = "test", Metadata = new Dictionary<string, string> { ["author"] = "John" } },
            new ContentRecord { Id = "3", Content = "test", Metadata = null! }
        };

        var results = records.AsQueryable().Where(expr).ToArray();

        // Only record 1 has the category field
        Assert.Single(results);
        Assert.Equal("1", results[0].Id);
    }

    [Fact]
    public void Build_NullHandling_DoesNotThrowOnNullFields()
    {
        var query = new ComparisonNode
        {
            Field = new FieldNode { FieldPath = "title" },
            Operator = ComparisonOperator.Contains,
            Value = new LiteralNode { Value = "test" }
        };

        var expr = this._builder.Build<ContentRecord>(query);

        var records = new[]
        {
            new ContentRecord { Id = "1", Title = "test guide", Content = "content" },
            new ContentRecord { Id = "2", Title = null!, Content = "content" },
            new ContentRecord { Id = "3", Title = "another test", Content = "content" }
        };

        // Should not throw on null title
        var results = records.AsQueryable().Where(expr).ToArray();

        Assert.Equal(2, results.Length);
        Assert.Contains(results, r => r.Id == "1");
        Assert.Contains(results, r => r.Id == "3");
    }

    [Fact]
    public void Build_ComplexNestedQuery_GeneratesCorrectLinq()
    {
        // (tags:AI OR tags:ML) AND content:kubernetes AND NOT mimeType:image/png
        var query = new LogicalNode
        {
            Operator = LogicalOperator.And,
            Children = new QueryNode[]
            {
                new LogicalNode
                {
                    Operator = LogicalOperator.Or,
                    Children = new QueryNode[]
                    {
                        new ComparisonNode
                        {
                            Field = new FieldNode { FieldPath = "tags" },
                            Operator = ComparisonOperator.In,
                            Value = new LiteralNode { Value = new[] { "AI" } }
                        },
                        new ComparisonNode
                        {
                            Field = new FieldNode { FieldPath = "tags" },
                            Operator = ComparisonOperator.In,
                            Value = new LiteralNode { Value = new[] { "ML" } }
                        }
                    }
                },
                new ComparisonNode
                {
                    Field = new FieldNode { FieldPath = "content" },
                    Operator = ComparisonOperator.Contains,
                    Value = new LiteralNode { Value = "kubernetes" }
                },
                new LogicalNode
                {
                    Operator = LogicalOperator.Not,
                    Children = new QueryNode[]
                    {
                        new ComparisonNode
                        {
                            Field = new FieldNode { FieldPath = "mimeType" },
                            Operator = ComparisonOperator.Equal,
                            Value = new LiteralNode { Value = "image/png" }
                        }
                    }
                }
            }
        };

        var expr = this._builder.Build<ContentRecord>(query);

        var records = new[]
        {
            new ContentRecord { Id = "1", Content = "kubernetes guide", Tags = new[] { "AI" }, MimeType = "text/plain" },
            new ContentRecord { Id = "2", Content = "kubernetes guide", Tags = new[] { "AI" }, MimeType = "image/png" },
            new ContentRecord { Id = "3", Content = "kubernetes guide", Tags = new[] { "docker" }, MimeType = "text/plain" },
            new ContentRecord { Id = "4", Content = "docker guide", Tags = new[] { "ML" }, MimeType = "text/plain" }
        };

        var results = records.AsQueryable().Where(expr).ToArray();

        // Only record 1 matches all conditions
        Assert.Single(results);
        Assert.Equal("1", results[0].Id);
    }
}
