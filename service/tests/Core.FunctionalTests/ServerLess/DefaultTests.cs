// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KM.Core.FunctionalTests.DefaultTestCases;
using Microsoft.KM.TestHelpers;
using Xunit.Abstractions;

namespace Microsoft.KM.Core.FunctionalTests.ServerLess;

public class DefaultTests : BaseFunctionalTestCase
{
    public DefaultTests(IConfiguration cfg, ITestOutputHelper output) : base(cfg, output)
    {
    }

    [Theory]
    [Trait("Category", "Serverless")]
    [InlineData("default")]
    [InlineData("simple_on_disk")]
    [InlineData("simple_volatile")]
    public async Task ItSupportsASingleFilter(string memoryType)
    {
        await FilteringTest.ItSupportsASingleFilter(this.GetServerlessMemory(memoryType), this.Log);
    }

    [Theory]
    [Trait("Category", "Serverless")]
    [InlineData("default")]
    [InlineData("simple_on_disk")]
    [InlineData("simple_volatile")]
    public async Task ItSupportsMultipleFilters(string memoryType)
    {
        await FilteringTest.ItSupportsMultipleFilters(this.GetServerlessMemory(memoryType), this.Log);
    }

    [Theory]
    [Trait("Category", "Serverless")]
    [InlineData("default")]
    [InlineData("simple_on_disk")]
    [InlineData("simple_volatile")]
    public async Task ItIgnoresEmptyFilters(string memoryType)
    {
        await FilteringTest.ItIgnoresEmptyFilters(this.GetServerlessMemory(memoryType), this.Log, true);
    }

    [Theory]
    [Trait("Category", "Serverless")]
    [InlineData("default")]
    [InlineData("simple_on_disk")]
    [InlineData("simple_volatile")]
    public async Task ItListsIndexes(string memoryType)
    {
        await IndexListTest.ItListsIndexes(this.GetServerlessMemory(memoryType), this.Log);
    }

    [Theory]
    [Trait("Category", "Serverless")]
    [InlineData("default")]
    [InlineData("simple_on_disk")]
    [InlineData("simple_volatile")]
    public async Task ItNormalizesIndexNames(string memoryType)
    {
        await IndexListTest.ItNormalizesIndexNames(this.GetServerlessMemory(memoryType), this.Log);
    }

    [Theory]
    [Trait("Category", "Serverless")]
    [InlineData("default")]
    [InlineData("simple_on_disk")]
    [InlineData("simple_volatile")]
    public async Task ItDeletesIndexes(string memoryType)
    {
        await IndexDeletionTest.ItDeletesIndexes(this.GetServerlessMemory(memoryType), this.Log);
    }

    [Theory]
    [Trait("Category", "Serverless")]
    [InlineData("default")]
    [InlineData("simple_on_disk")]
    [InlineData("simple_volatile")]
    public async Task ItHandlesMissingIndexesConsistently(string memoryType)
    {
        await MissingIndexTest.ItHandlesMissingIndexesConsistently(this.GetServerlessMemory(memoryType), this.Log);
    }

    [Theory]
    [Trait("Category", "Serverless")]
    [InlineData("default")]
    [InlineData("simple_on_disk")]
    [InlineData("simple_volatile")]
    public async Task ItUploadsPDFDocsAndDeletes(string memoryType)
    {
        await DocumentUploadTest.ItUploadsPDFDocsAndDeletes(this.GetServerlessMemory(memoryType), this.Log);
    }

    [Theory]
    [Trait("Category", "Serverless")]
    [InlineData("default")]
    [InlineData("simple_on_disk")]
    [InlineData("simple_volatile")]
    public async Task ItSupportsTags(string memoryType)
    {
        await DocumentUploadTest.ItSupportsTags(this.GetServerlessMemory(memoryType), this.Log);
    }

    [Theory]
    [Trait("Category", "Serverless")]
    [InlineData("default")]
    [InlineData("simple_on_disk")]
    [InlineData("simple_volatile")]
    public async Task ItDownloadsPDFDocs(string memoryType)
    {
        await DocumentUploadTest.ItDownloadsPDFDocs(this.GetServerlessMemory(memoryType), this.Log);
    }
}
