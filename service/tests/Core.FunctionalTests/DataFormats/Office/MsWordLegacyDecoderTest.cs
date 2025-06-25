// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Configuration;
using Microsoft.KernelMemory.DataFormats;
using Microsoft.KernelMemory.DataFormats.Office;
using Microsoft.KM.TestHelpers;

namespace Microsoft.KM.Core.FunctionalTests.DataFormats.Office;

public class MsWordLegacyDecoderTest : BaseFunctionalTestCase
{
    public MsWordLegacyDecoderTest(IConfiguration cfg, ITestOutputHelper output) : base(cfg, output)
    {
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "DataFormats")]
    [Trait("Category", "WordLegacy")]
    public async Task ItExtractsTextFromDocFile()
    {
        // Arrange
        const string file = "file4-sample-legacy-word.doc";
        var decoder = new MsWordLegacyDecoder();

        // Act
        FileContent result = await decoder.DecodeAsync(file);
        string content = result.Sections.Aggregate("", (current, s) => current + (s.Content + "\n"));
        Console.WriteLine(content);

        // Assert
        Assert.NotEmpty(content);
        Assert.True(result.Sections.Count > 0);
        Assert.Contains("Retrieval Augmented Generation (RAG) is an architecture", content);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "DataFormats")]
    [Trait("Category", "WordLegacy")]
    public void ItSupportsMsWordMimeType()
    {
        // Arrange
        var decoder = new MsWordLegacyDecoder();

        // Act & Assert
        Assert.True(decoder.SupportsMimeType("application/msword"));
        Assert.False(decoder.SupportsMimeType("application/vnd.openxmlformats-officedocument.wordprocessingml.document"));
        Assert.False(decoder.SupportsMimeType("application/pdf"));
    }
}
