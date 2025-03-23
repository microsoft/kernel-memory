// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Chunkers.UnitTests.Helpers;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.Chunkers;
using Microsoft.KernelMemory.DataFormats;
using Microsoft.KM.TestHelpers;

namespace Microsoft.Chunkers.UnitTests;

public class MarkDownChunkerTests(ITestOutputHelper output) : BaseUnitTestCase(output)
{
    private static readonly MarkDownChunker chunker1 = new(new OneCharTestTokenizer());
    private static readonly MarkDownChunker chunker2 = new(new TwoCharsTestTokenizer());
    private static readonly MarkDownChunker chunker4 = new(new FourCharsTestTokenizer());

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "Chunking")]
    public void CanSplitMarkdownParagraphs()
    {
        // Arrange
        List<string> input =
        [
            "This is a test of the emergency broadcast system. This is only a test.",
            "We repeat, this is only a test. A unit test."
        ];
        string[] expected =
        [
            "This is a test of the emergency broadcast system. ",
            "This is only a test. ",
            "We repeat, this is only a test. A unit test."
        ];

        // Act
        var chunks = chunker4.Split(string.Join(' ', input), new MarkDownChunkerOptions { MaxTokensPerChunk = 13 });
        DebugChunks(chunks, new FourCharsTestTokenizer());

        // Assert
        Assert.Equal(expected, chunks);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "Chunking")]
    public void CanSplitMarkdownParagraphsWithOverlap()
    {
        // Arrange
        List<string> input =
        [
            "This is a test of the emergency broadcast system. This is only a test.",
            "We repeat, this is only a test. A unit test."
        ];

        string[] expected =
        [
            "This is a test of the emergency broadcast system. ",
            "e emergency broadcast system. This is only a test. ",
            "This is only a test. We repeat, ",
            "We repeat, this is only a test. A unit ",
            "this is only a test. A unit test."
        ];

        // Act
        var chunks = chunker4.Split(string.Join(' ', input), new MarkDownChunkerOptions { MaxTokensPerChunk = 15, Overlap = 8 });
        DebugChunks(chunks, new FourCharsTestTokenizer());

        // Assert
        Assert.Equal(expected, chunks);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "Chunking")]
    public void CanSplitMarkDownLines()
    {
        // Arrange
        const string Input = "This is a test of the emergency broadcast system. This is only a test.";
        string[] expected =
        [
            "This is a test of the emergency broadcast system. ",
            "This is only a test."
        ];

        var result = chunker4.Split(Input, 15);

        Assert.Equal(expected, result);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "Chunking")]
    public void CanSplitMarkdownParagraphsWithEmptyInput()
    {
        // Assert
        var expected = new List<string>();

        // Act
        var chunks = chunker4.Split("", 13);
        DebugChunks(chunks, new FourCharsTestTokenizer());

        // Assert
        Assert.Equal(expected, chunks);
    }

    // A markdown example that splits on '\r' or '\n'
    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "Chunking")]
    public void CanSplitMarkdownParagraphsOnNewlines()
    {
        // Arrange
        const string Separator = "\n";
        List<string> input =
        [
            "This_is_a_test_of_the_emergency_broadcast_system\r\nThis_is_only_a_test",
            "We_repeat_this_is_only_a_test\nA_unit_test",
            "A_small_note\nAnd_another\r\nAnd_once_again\rSeriously_this_is_the_end\nWe're_finished\nAll_set\nBye\n",
            "Done"
        ];

        List<string> expected4 =
        [
            $"This_is_a_test_of_the_emergency_broadcast_system{Separator}", // 13 tokens
            $"This_is_only_a_test{Separator}We_repeat_this_is_only_a_test{Separator}", // 13 tokens
            $"A_unit_test{Separator}A_small_note{Separator}And_another{Separator}And_once_again{Separator}", // 13 tokens
            $"Seriously_this_is_the_end{Separator}We're_finished{Separator}All_set{Separator}Bye{Separator}{Separator}Done" // 15 tokens
        ];

        List<string> expected2 =
        [
            $"This_is_a_test_of_the_emergency_broadcast_system{Separator}", // 25 tokens
            $"This_is_only_a_test\nWe_repeat_this_is_only_a_test{Separator}", // 25 tokens
            $"A_unit_test\nA_small_note\nAnd_another\nAnd_once_again{Separator}", // 26 tokens
            $"Seriously_this_is_the_end{Separator}We're_finished{Separator}All_set{Separator}Bye{Separator}{Separator}Done" // 29 tokens
        ];

        // Act
        List<string> chunks4 = chunker4.Split(string.Join(Separator, input), 15);
        DebugChunks(chunks4, new FourCharsTestTokenizer());

        List<string> chunks2 = chunker2.Split(string.Join(Separator, input), 30);
        DebugChunks(chunks2, new TwoCharsTestTokenizer());

        // Assert
        Assert.Equal(expected4, chunks4);
        Assert.Equal(expected2, chunks2);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "Chunking")]
    public void CanSplitMarkdownParagraphsWithCustomTokenCounter()
    {
        // Arrange
        const string Separator = "\n";
        List<string> input =
        [
            "This is a test of the emergency broadcast system. This is only a test.",
            "We repeat, this is only a test. A unit test."
        ];
        string[] expected =
        [
            "This is a test of the emergency broadcast system. ", // 50 tokens
            $"This is only a test.{Separator}", // 21 tokens
            "We repeat, this is only a test. A unit test." // 44 tokens
        ];

        // Act
        var chunks = chunker1.Split(string.Join(Separator, input), 52);
        DebugChunks(chunks, new OneCharTestTokenizer());

        // Assert
        Assert.Equal(expected, chunks);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "Chunking")]
    public void CanSplitMarkdownParagraphsWithOverlapAndCustomTokenCounter()
    {
        // Arrange
        const string Separator = "\n";
        List<string> input =
        [
            "This is a test of the emergency broadcast system. This is only a test.",
            "We repeat, this is only a test. A unit test."
        ];

        string[] expected =
        [
            $"This is a test of the emergency broadcast system. This is only a test.{Separator}", // 71 tokens
            $" broadcast system. This is only a test.{Separator}We repeat, this is only a test. ", // 72 tokens
            "We repeat, this is only a test. A unit test." // 44 tokens
        ];

        // Act
        var chunks = chunker1.Split(string.Join(Separator, input), new MarkDownChunkerOptions { MaxTokensPerChunk = 75, Overlap = 40 });
        DebugChunks(chunks, new OneCharTestTokenizer());

        // Assert
        Assert.Equal(expected, chunks);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "Chunking")]
    public void CanSplitMarkDownLinesWithCustomTokenCounter()
    {
        // Arrange
        const string Input = "This is a test of the emergency broadcast system. This is only a test.";
        string[] expected =
        [
            "This is a test of the emergency broadcast system. ",
            "This is only a test."
        ];

        // Act
        var chunks = chunker1.Split(Input, 60);
        DebugChunks(chunks, new OneCharTestTokenizer());

        // Assert
        Assert.Equal(expected, chunks);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "Chunking")]
    public void ItSplitsMarkdownLines()
    {
        // Arrange
        var line = "This is a test of the emergency broadcast system. This is only a test.";

        // Act
        var chunks4 = chunker4.Split(line, 20);
        var chunks2 = chunker2.Split(line, 20);
        DebugChunks(chunks4, new FourCharsTestTokenizer());
        DebugChunks(chunks2, new TwoCharsTestTokenizer());

        // Assert
        Assert.Equal(1, chunks4.Count);
        Assert.Equal(2, chunks2.Count);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "Chunking")]
    public void CanSplitMarkdownParagraphsWithHeader()
    {
        // Arrange
        const string Separator = "\n";
        const string ChunkHeader = "DOCUMENT NAME: test.txt\n\n";
        List<string> input =
        [
            "This is a test of the emergency broadcast system. This is only a test.",
            "We repeat, this is only a test. A unit test."
        ];
        List<string> expected =
        [
            $"{ChunkHeader}This is a test of the emergency broadcast system. ",
            $"{ChunkHeader}This is only a test.{Separator}",
            $"{ChunkHeader}We repeat, this is only a test. A unit test."
        ];

        // Act
        var chunks = new MarkDownChunker().Split(string.Join(Separator, input), new MarkDownChunkerOptions { MaxTokensPerChunk = 20, ChunkHeader = ChunkHeader });
        DebugChunks(chunks, new CL100KTokenizer());

        // Assert
        Assert.Equal(expected, chunks);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "Chunking")]
    public void CanSplitMarkdownParagraphsWithOverlapAndHeader()
    {
        // Arrange
        const string Separator = "\n";
        const string ChunkHeader = "DOCUMENT NAME: test.txt\n\n";
        List<string> input =
        [
            "This is a test of the emergency broadcast system. This is only a test.",
            "We repeat, this is only a test. A unit test."
        ];

        List<string> expected =
        [
            $"{ChunkHeader}This is a test of the emergency broadcast system. ", // 19 tokens
            $"{ChunkHeader}e emergency broadcast system. This is only a test.{Separator}", // 19 tokens
            $"{ChunkHeader}This is only a test.{Separator}We repeat, ", // 15 tokens
            $"{ChunkHeader}We repeat, this is only a test. A unit ", // 16 tokens
            $"{ChunkHeader}this is only a test. A unit test." // 15 tokens
        ];

        // Act
        var chunks = chunker4.Split(string.Join(Separator, input), new MarkDownChunkerOptions { MaxTokensPerChunk = 22, Overlap = 8, ChunkHeader = ChunkHeader });
        DebugChunks(chunks, new FourCharsTestTokenizer());

        // Assert
        Assert.Equal(expected, chunks);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "Chunking")]
    public void CanSplitMarkdownParagraphsWithHeaderAndCustomTokenCounter()
    {
        // Arrange
        const string Separator = "\n";
        const string ChunkHeader = "DOCUMENT NAME: test.txt\n\n";
        List<string> input =
        [
            "This is a test of the emergency broadcast system. This is only a test.",
            "We repeat, this is only a test. A unit test."
        ];
        string[] expected =
        [
            $"{ChunkHeader}This is a test of the emergency broadcast system. ",
            $"{ChunkHeader}This is only a test.{Separator}",
            $"{ChunkHeader}We repeat, this is only a test. A unit test."
        ];

        // Act
        var chunks = chunker1.Split(string.Join(Separator, input), new MarkDownChunkerOptions { MaxTokensPerChunk = 77, ChunkHeader = ChunkHeader });
        DebugChunks(chunks, new OneCharTestTokenizer());

        // Assert
        Assert.Equal(expected, chunks);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "Chunking")]
    public void CanSplitMarkdownParagraphsWithOverlapAndHeaderAndCustomTokenCounter()
    {
        // Arrange
        const string Separator = "\n";
        const string ChunkHeader = "DOCUMENT NAME: test.txt\n\n";
        List<string> input =
        [
            "This is a test of the emergency broadcast system. This is only a test.",
            "We repeat, this is only a test. A unit test."
        ];

        string[] expected =
        [
            $"{ChunkHeader}This is a test of the emergency broadcast system. This is only a test.{Separator}", // 96 tokens
            $"{ChunkHeader} broadcast system. This is only a test.{Separator}We repeat, this is only a test. ", // 97 tokens
            $"{ChunkHeader}We repeat, this is only a test. A unit test." // 69 tokens
        ];

        // Act
        var chunks = chunker1.Split(string.Join(Separator, input), new MarkDownChunkerOptions { MaxTokensPerChunk = 100, Overlap = 40, ChunkHeader = ChunkHeader });
        DebugChunks(chunks, new OneCharTestTokenizer());

        // Assert
        Assert.Equal(expected, chunks);
    }

    #region internals

    private static void DebugChunks(IEnumerable<string> chunks, ITextTokenizer tokenizer)
    {
        var list = chunks.ToList();

        Console.WriteLine("----------------------------------");
        for (int index = 0; index < list.Count; index++)
        {
            Console.WriteLine($"- {index}: \"{list[index]}\" [{tokenizer.CountTokens(list[index])} tokens]");
        }

        Console.WriteLine("----------------------------------");
    }

    private static void DebugFragments(List<Chunk> fragments)
    {
        if (fragments.Count == 0)
        {
            Console.WriteLine("No tokens in the list.");
        }

        for (int index = 0; index < fragments.Count; index++)
        {
            Console.WriteLine($"- {index}: Value: \"{fragments[index].Content}\"");
        }
    }

    #endregion
}
