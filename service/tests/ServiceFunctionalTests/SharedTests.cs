// Copyright (c) Microsoft. All rights reserved.

using FunctionalTests.Scenarios;
using Microsoft.KernelMemory;
using Microsoft.TestHelpers;
using Xunit.Abstractions;

namespace ServiceFunctionalTests;

public class SharedTests : BaseFunctionalTestCase
{
    private readonly IKernelMemory _memory;

    public SharedTests(IConfiguration cfg, ITestOutputHelper output) : base(cfg, output)
    {
        this._memory = this.GetMemoryWebClient();
    }

    [Fact]
    [Trait("Category", "WebService")]
    public async Task ItSupportsASingleFilter()
    {
        await SharedFilteringTest.ItSupportsASingleFilter(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "WebService")]
    public async Task ItSupportsMultipleFilters()
    {
        await SharedFilteringTest.ItSupportsMultipleFilters(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "WebService")]
    public async Task ItIgnoresEmptyFilters()
    {
        await SharedFilteringTest.ItIgnoresEmptyFilters(this._memory, this.Log, true);
    }

    [Fact]
    [Trait("Category", "WebService")]
    public async Task ItListsIndexes()
    {
        await SharedIndexListTest.ItListsIndexes(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "WebService")]
    public async Task ItNormalizesIndexNames()
    {
        await SharedIndexListTest.ItNormalizesIndexNames(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "WebService")]
    public async Task ItDeletesIndexes()
    {
        await SharedIndexDeletionTest.ItDeletesIndexes(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "WebService")]
    public async Task ItHandlesMissingIndexesConsistently()
    {
        await SharedMissingIndexTest.ItHandlesMissingIndexesConsistently(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "WebService")]
    public async Task ItUploadsPDFDocsAndDeletes()
    {
        await SharedDocumentUploadTest.ItUploadsPDFDocsAndDeletes(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "WebService")]
    public async Task ItSupportsTags()
    {
        await SharedDocumentUploadTest.ItSupportsTags(this._memory, this.Log);
    }
}
