// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Configuration;
using Microsoft.KernelMemory.DataFormats;
using Microsoft.KernelMemory.DataFormats.Office;
using Microsoft.KM.TestHelpers;

namespace Microsoft.KM.Core.FunctionalTests.DataFormats.Office;

public class MsExcelDecoderTest : BaseFunctionalTestCase
{
    public MsExcelDecoderTest(IConfiguration cfg, ITestOutputHelper output) : base(cfg, output)
    {
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "DataFormats")]
    [Trait("Category", "Excel")]
    public async Task ItExtractsAllTypes()
    {
        // Arrange
        const string file = "file3-data.xlsx";
        var decoder = new MsExcelDecoder();

        // Act
        FileContent result = await decoder.DecodeAsync(file);
        string content = result.Sections.Aggregate("", (current, s) => current + (s.Content + "\n"));
        Console.WriteLine(content);

        // Assert
        Assert.Contains("\"0.5\"", content); // 50% percentage
        Assert.Contains("\"512.99\"", content); // number
        Assert.Contains("\"3.99999999\"", content); // number
        Assert.Contains("\"0.25\"", content); // fraction
        Assert.Contains("\"123.6\"", content); // currency
        Assert.Contains("\"4518\"", content); // currency
        Assert.Contains("\"444666\"", content); // currency
        Assert.Contains("\"United States of America\"", content); // text
        Assert.Contains("\"Rome\", \"\", \"Tokyo\"", content); // text with empty columns
        Assert.Contains("\"12/25/2090\"", content); // date
        Assert.Contains("\"98001\"", content); // zip code
        Assert.Contains("\"15554000600\"", content); // phone number
        Assert.Contains("\"TRUE\"", content); // boolean
        Assert.True(content.Contains("\"1/12/2009\"") || content.Contains("\"01/12/2009\""), // date
            $"{content} doesn't match one of the expected formats");
    }
}
