// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Configuration;
using Microsoft.KernelMemory;
using Microsoft.KM.Core.FunctionalTests.DefaultTestCases;
using Microsoft.KM.TestHelpers;

namespace Microsoft.KM.Service.FunctionalTests;

public class DefaultTests : BaseFunctionalTestCase
{
    private readonly IKernelMemory _memory;

    public DefaultTests(IConfiguration cfg, ITestOutputHelper output) : base(cfg, output)
    {
        this._memory = this.GetMemoryWebClient();
    }

    [Fact]
    [Trait("Category", "WebService")]
    public async Task ItSupportsASingleFilter()
    {
        await FilteringTest.ItSupportsASingleFilter(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "WebService")]
    public async Task ItSupportsMultipleFilters()
    {
        await FilteringTest.ItSupportsMultipleFilters(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "WebService")]
    public async Task ItIgnoresEmptyFilters()
    {
        await FilteringTest.ItIgnoresEmptyFilters(this._memory, this.Log, true);
    }

    [Fact]
    [Trait("Category", "WebService")]
    public async Task ItDoesntFailIfTheIndexExistsAlready()
    {
        await IndexCreationTest.ItDoesntFailIfTheIndexExistsAlready(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "WebService")]
    public async Task ItListsIndexes()
    {
        await IndexListTest.ItListsIndexes(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "WebService")]
    public async Task ItNormalizesIndexNames()
    {
        await IndexListTest.ItNormalizesIndexNames(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "WebService")]
    public async Task ItUsesDefaultIndexName()
    {
        await IndexListTest.ItUsesDefaultIndexName(this._memory, this.Log, "default");
    }

    [Fact]
    [Trait("Category", "WebService")]
    public async Task ItDeletesIndexes()
    {
        await IndexDeletionTest.ItDeletesIndexes(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "WebService")]
    public async Task ItHandlesMissingIndexesConsistently()
    {
        await MissingIndexTest.ItHandlesMissingIndexesConsistently(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "WebService")]
    public async Task ItUploadsPDFDocsAndDeletes()
    {
        await DocumentUploadTest.ItUploadsPDFDocsAndDeletes(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "WebService")]
    public async Task ItSupportsTags()
    {
        await DocumentUploadTest.ItSupportsTags(this._memory, this.Log);
    }

    [Fact]
    [Trait("Category", "WebService")]
    public async Task ItDownloadsPDFDocs()
    {
        await DocumentUploadTest.ItDownloadsPDFDocs(this._memory, this.Log);
    }
}
