// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KM.TestHelpers;
using Xunit.Abstractions;

namespace Microsoft.KM.Service.FunctionalTests;

public class ImageOCRTest : BaseFunctionalTestCase
{
    private readonly IKernelMemory _memory;
    private readonly string? _fixturesPath;

    public ImageOCRTest(
        IConfiguration cfg,
        ITestOutputHelper output) : base(cfg, output)
    {
        this._fixturesPath = this.FindFixturesDir();
        Assert.NotNull(this._fixturesPath);
        Console.WriteLine($"\n# Fixtures directory found: {this._fixturesPath}");

        this._memory = this.GetMemoryWebClient();
    }

    [Fact]
    [Trait("Category", "WebService")]
    public async Task ItUsesTextFoundInsideImages()
    {
        // Arrange
        const string DocId = nameof(this.ItUsesTextFoundInsideImages);
        await this._memory.ImportDocumentAsync(new Document(DocId)
            .AddFiles(new[] { Path.Join(this._fixturesPath, "ANWC-image-for-OCR.jpg") }));

        // Wait
        while (!await this._memory.IsDocumentReadyAsync(documentId: DocId))
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        // Act
        var answer = await this._memory.AskAsync("Who is sponsoring the Automotive News World Congress?");

        // Assert
        Console.WriteLine(answer.Result);
        Assert.Contains("Microsoft", answer.Result);
    }
}
