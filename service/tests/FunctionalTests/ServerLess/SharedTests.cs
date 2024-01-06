// Copyright (c) Microsoft. All rights reserved.

using FunctionalTests.Scenarios;
using Microsoft.TestHelpers;
using Xunit.Abstractions;

namespace FunctionalTests.ServerLess;

public class SharedTests : BaseFunctionalTestCase
{
    public SharedTests(IConfiguration cfg, ITestOutputHelper output) : base(cfg, output)
    {
    }

    [Theory]
    [Trait("Category", "Serverless")]
    [InlineData("default")]
    [InlineData("simple_on_disk")]
    [InlineData("simple_volatile")]
    public async Task ItSupportsASingleFilter(string memoryType)
    {
        await SharedFilteringTest.ItSupportsASingleFilter(this.GetServerlessMemory(memoryType), this.Log);
    }

    [Theory]
    [Trait("Category", "Serverless")]
    [InlineData("default")]
    [InlineData("simple_on_disk")]
    [InlineData("simple_volatile")]
    public async Task ItSupportsMultipleFilters(string memoryType)
    {
        await SharedFilteringTest.ItSupportsMultipleFilters(this.GetServerlessMemory(memoryType), this.Log);
    }

    [Theory]
    [Trait("Category", "Serverless")]
    [InlineData("default")]
    [InlineData("simple_on_disk")]
    [InlineData("simple_volatile")]
    public async Task ItIgnoresEmptyFilters(string memoryType)
    {
        await SharedFilteringTest.ItIgnoresEmptyFilters(this.GetServerlessMemory(memoryType), this.Log, true);
    }

    [Theory]
    [Trait("Category", "Serverless")]
    [InlineData("default")]
    [InlineData("simple_on_disk")]
    [InlineData("simple_volatile")]
    public async Task ItListsIndexes(string memoryType)
    {
        await SharedIndexListTest.ItListsIndexes(this.GetServerlessMemory(memoryType), this.Log);
    }

    [Theory]
    [Trait("Category", "Serverless")]
    [InlineData("default")]
    [InlineData("simple_on_disk")]
    [InlineData("simple_volatile")]
    public async Task ItNormalizesIndexNames(string memoryType)
    {
        await SharedIndexListTest.ItNormalizesIndexNames(this.GetServerlessMemory(memoryType), this.Log);
    }

    [Theory]
    [Trait("Category", "Serverless")]
    [InlineData("default")]
    [InlineData("simple_on_disk")]
    [InlineData("simple_volatile")]
    public async Task ItDeletesIndexes(string memoryType)
    {
        await SharedIndexDeletionTest.ItDeletesIndexes(this.GetServerlessMemory(memoryType), this.Log);
    }

    [Theory]
    [Trait("Category", "Serverless")]
    [InlineData("default")]
    [InlineData("simple_on_disk")]
    [InlineData("simple_volatile")]
    public async Task ItHandlesMissingIndexesConsistently(string memoryType)
    {
        await SharedMissingIndexTest.ItHandlesMissingIndexesConsistently(this.GetServerlessMemory(memoryType), this.Log);
    }

    [Theory]
    [Trait("Category", "Serverless")]
    [InlineData("default")]
    [InlineData("simple_on_disk")]
    [InlineData("simple_volatile")]
    public async Task ItUploadsPDFDocsAndDeletes(string memoryType)
    {
        await SharedDocumentUploadTest.ItUploadsPDFDocsAndDeletes(this.GetServerlessMemory(memoryType), this.Log);
    }

    [Theory]
    [Trait("Category", "Serverless")]
    [InlineData("default")]
    [InlineData("simple_on_disk")]
    [InlineData("simple_volatile")]
    public async Task ItSupportsTags(string memoryType)
    {
        await SharedDocumentUploadTest.ItSupportsTags(this.GetServerlessMemory(memoryType), this.Log);
    }
}
