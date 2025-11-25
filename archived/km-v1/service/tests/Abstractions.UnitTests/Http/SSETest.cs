// Copyright (c) Microsoft. All rights reserved.

using System.Text;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.HTTP;

namespace Microsoft.KM.Abstractions.UnitTests.Http;

[Trait("Category", "UnitTest")]
public class SSETest
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("   \n")]
    public void ItParsesEmptyStrings(string input)
    {
        Assert.Null(SSE.ParseMessage<MemoryAnswer>(input));
    }

    [Fact]
    public void ItParsesSingleLineMessage()
    {
        // Arrange
        var message = """
                      data: { "question": "q" }
                      """;

        // Act
        var x = SSE.ParseMessage<MemoryAnswer>(message);

        // Assert
        Assert.NotNull(x);
        Assert.Equal("q", x.Question);
    }

    [Fact]
    public void ItParsesSingleLineMessageWithSeparator()
    {
        // Arrange
        var message = """
                      data: { "question": "q" }


                      """;

        // Act
        var x = SSE.ParseMessage<MemoryAnswer>(message);

        // Assert
        Assert.NotNull(x);
        Assert.Equal("q", x.Question);
    }

    [Fact]
    public void ItParsesMultiLineMessage()
    {
        // Arrange
        var message = """
                      data: { "question": "q"
                      data: , "noResultReason": "abc"
                      data: }
                      """;

        // Act
        var x = SSE.ParseMessage<MemoryAnswer>(message);

        // Assert
        Assert.NotNull(x);
        Assert.Equal("q", x.Question);
        Assert.Equal("abc", x.NoResultReason);
    }

    [Theory]
    [InlineData("data: [DONE]")]
    [InlineData("data: [DONE]\n")]
    [InlineData("data: [DONE]\n\n")]
    public async Task ItParsesEmptyStreams(string input)
    {
        // Arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(input));

        // Act
        var result = SSE.ParseStreamAsync<MemoryAnswer>(stream);

        // Assert
        var messages = new List<MemoryAnswer>();
        await foreach (var message in result)
        {
            messages.Add(message);
        }

        Assert.Equal(0, messages.Count);
    }

    [Theory]
    [InlineData("data: { \"question\": \"qq\" }")]
    [InlineData("data: { \"question\": \"qq\" }\n")]
    [InlineData("data: { \"question\": \"qq\" }\n\n")]
    [InlineData("data: { \"question\": \"qq\" }\n\ndata: [DONE]")]
    [InlineData("data: { \"question\": \"qq\" }\n\ndata: [DONE]\n")]
    [InlineData("data: { \"question\": \"qq\" }\n\ndata: [DONE]\n\n")]
    public async Task ItParsesStreamsWithASingleMessage(string input)
    {
        // Arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(input));

        // Act
        var result = SSE.ParseStreamAsync<MemoryAnswer>(stream);

        // Assert
        var messages = new List<MemoryAnswer>();
        await foreach (var message in result)
        {
            messages.Add(message);
        }

        Assert.Equal(1, messages.Count);
        Assert.NotNull(messages[0]);
        Assert.Equal("qq", messages[0].Question);
    }

    [Theory]
    [InlineData("data: { \"question\": \"qq\" }\n\ndata: { \"question\": \"kk\" }\n\n")]
    [InlineData("data: { \"question\": \"qq\" }\n\ndata: { \"question\": \"kk\" }\n\ndata: [DONE]")]
    [InlineData("data: { \"question\": \"qq\" }\n\ndata: { \"question\": \"kk\" }\n\ndata: [DONE]\n")]
    [InlineData("data: { \"question\": \"qq\" }\n\ndata: { \"question\": \"kk\" }\n\ndata: [DONE]\n\n")]
    public async Task ItParsesStreamsWithMultipleMessage(string input)
    {
        // Arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(input));

        // Act
        var result = SSE.ParseStreamAsync<MemoryAnswer>(stream);

        // Assert
        var messages = new List<MemoryAnswer>();
        await foreach (var message in result)
        {
            messages.Add(message);
        }

        Assert.Equal(2, messages.Count);
        Assert.NotNull(messages[0]);
        Assert.Equal("qq", messages[0].Question);
        Assert.NotNull(messages[1]);
        Assert.Equal("kk", messages[1].Question);
    }
}
