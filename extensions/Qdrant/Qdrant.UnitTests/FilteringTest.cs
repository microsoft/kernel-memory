// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json;
using Microsoft.KernelMemory.MemoryDb.Qdrant.Client.Http;
using Microsoft.KM.TestHelpers;
using Xunit.Abstractions;

namespace Microsoft.Qdrant.UnitTests;

public class FilteringTest : BaseUnitTestCase
{
    public FilteringTest(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "Qdrant")]
    public void FiltersAreRenderedToJson()
    {
        const string Expected = """
                                {
                                    "should":
                                    [
                                        {
                                            "must":
                                            [
                                                {
                                                    "key": "tags",
                                                    "match":
                                                    {
                                                        "value": "user:devis"
                                                    }
                                                },
                                                {
                                                    "key": "tags",
                                                    "match":
                                                    {
                                                        "value": "type:chat"
                                                    }
                                                }
                                            ]
                                        },
                                        {
                                            "must":
                                            [
                                                {
                                                    "key": "tags",
                                                    "match":
                                                    {
                                                        "value": "user:taylor"
                                                    }
                                                },
                                                {
                                                    "key": "tags",
                                                    "match":
                                                    {
                                                        "value": "type:blog"
                                                    }
                                                }
                                            ]
                                        }
                                    ]
                                }
                                """;

        // Arrange
        var filter1 = new Filter.AndClause().AndValue("tags", "user:devis").AndValue("tags", "type:chat");
        var filter2 = new Filter.AndClause().AndValue("tags", "user:taylor").AndValue("tags", "type:blog");
        var combined = new Filter.OrClause().Or(filter1).Or(filter2);

        // Act
        var actual = JsonSerializer.Serialize(combined);

        // Assert
        var expected = JsonSerializer.Serialize(JsonSerializer.Deserialize<object>(Expected));
        Assert.Equal(expected, actual);
    }
}
