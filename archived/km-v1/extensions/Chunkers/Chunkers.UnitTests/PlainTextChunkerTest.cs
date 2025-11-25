// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Chunkers.UnitTests.Helpers;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.Chunkers;
using Microsoft.KernelMemory.Chunkers.internals;
using Microsoft.KernelMemory.DataFormats;
using Microsoft.KM.TestHelpers;

namespace Microsoft.Chunkers.UnitTests;

public class PlainTextChunkerTest(ITestOutputHelper output) : BaseUnitTestCase(output)
{
    private static readonly PlainTextChunker chunker1 = new(new OneCharTestTokenizer());
    private static readonly PlainTextChunker chunker2 = new(new TwoCharsTestTokenizer());
    private static readonly PlainTextChunker chunker4 = new(new FourCharsTestTokenizer());

    private static readonly SeparatorTrie s_separators = new([
        // Symbol + space
        ". ", ".\t", ".\n", // note: covers also the case of multiple '.' like "....\n"
        "? ", "?\t", "?\n", // note: covers also the case of multiple '?' and '!?' like "?????\n" and "?!?\n"
        "! ", "!\t", "!\n", // note: covers also the case of multiple '!' and '?!' like "!!!\n" and "!?!\n"
        "⁉ ", "⁉\t", "⁉\n",
        "⁈ ", "⁈\t", "⁈\n",
        "⁇ ", "⁇\t", "⁇\n",
        "… ", "…\t", "…\n",
        // Multi-char separators without space, ordered by length
        "!!!!", "????", "!!!", "???", "?!?", "!?!", "!?", "?!", "!!", "??", "....", "...", "..",
        // 1 char separators without space
        ".", "?", "!", "⁉", "⁈", "⁇", "…",

        "; ", ";\t", ";\n", ";",
        "} ", "}\t", "}\n", "}", // note: curly brace without spaces is up here because it's a common code ending char, more important than ')' or ']'
        ") ", ")\t", ")\n",
        "] ", "]\t", "]\n",
        ")", "]",

        ":", // note: \n \t make no difference with this char
        ",", // note: \n \t make no difference with this char
        " ", // note: \n \t make no difference with this char
        "-", // note: \n \t make no difference with this char
    ]);

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "Chunking")]
    public void ItTokenizesText()
    {
        // Arrange
        string text = "Hello, world!";

        // Act
        List<Chunk> fragments = new PlainTextChunker().SplitToFragments(text, s_separators);
        DebugFragments(fragments);

        // Assert
        Assert.Equal(5, fragments.Count);
        Assert.Equal("Hello", fragments[0].Content);
        Assert.Equal(",", fragments[1].Content);
        Assert.Equal(" ", fragments[2].Content);
        Assert.Equal("world", fragments[3].Content);
        Assert.Equal("!", fragments[4].Content);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "Chunking")]
    public void ItHandlesConsecutiveSentenceSeparators()
    {
        // Arrange
        string text = "Hello. . . world!!!!!!!!!!!!!";

        // Act
        List<Chunk> fragments = new PlainTextChunker().SplitToFragments(text, s_separators);
        DebugFragments(fragments);

        // Assert
        Assert.Equal(9, fragments.Count);
        Assert.Equal("Hello", fragments[0].Content);
        Assert.Equal(". ", fragments[1].Content);
        Assert.Equal(". ", fragments[2].Content);
        Assert.Equal(". ", fragments[3].Content);
        Assert.Equal("world", fragments[4].Content);
        Assert.Equal("!!!!", fragments[5].Content);
        Assert.Equal("!!!!", fragments[6].Content);
        Assert.Equal("!!!!", fragments[7].Content);
        Assert.Equal("!", fragments[8].Content);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "Chunking")]
    public void ItHandlesTailWithoutTermination1()
    {
        // Arrange
        string text = "Hello";

        // Act
        List<Chunk> fragments = new PlainTextChunker().SplitToFragments(text, s_separators);
        DebugFragments(fragments);

        // Assert
        Assert.Equal(1, fragments.Count);
        Assert.Equal("Hello", fragments[0].Content);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "Chunking")]
    public void ItHandlesTailWithoutTermination2()
    {
        // Arrange
        string text = "Hello!World";

        // Act
        List<Chunk> fragments = new PlainTextChunker().SplitToFragments(text, s_separators);
        DebugFragments(fragments);

        // Assert
        Assert.Equal(3, fragments.Count);
        Assert.Equal("Hello", fragments[0].Content);
        Assert.Equal("!", fragments[1].Content);
        Assert.Equal("World", fragments[2].Content);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "Chunking")]
    public void CanSplitPlainTextLines()
    {
        // Arrange
        const string Input = "This is a test of the emergency broadcast system. This is only a test.";
        string[] expected =
        [
            "This is a test of the emergency broadcast system. ",
            "This is only a test."
        ];

        // Act
        var chunks = chunker4.Split(Input, new PlainTextChunkerOptions { MaxTokensPerChunk = 15 });
        DebugChunks(chunks, new FourCharsTestTokenizer());

        // Assert
        Assert.Equal(expected, chunks);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "Chunking")]
    public void CanSplitPlainTextLinesWithCustomTokenCounter()
    {
        // Arrange
        const string Input = "This is a test of the emergency broadcast system. This is only a test.";
        string[] expected =
        [
            "This is a test of the emergency broadcast system. ",
            "This is only a test."
        ];

        // Act
        var chunks = chunker1.Split(Input, new PlainTextChunkerOptions { MaxTokensPerChunk = 60 });

        // Assert
        Assert.Equal(expected, chunks);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "Chunking")]
    public void TheFirstChunkIsNotEmptyWhenTheFirstSentenceIsLong()
    {
        // Arrange
        const string Input = "This is a sentence longer than 5 tokens, as you can see.";

        string[] expected =
        [
            "This is a sentence ",
            "longer than 5 ",
            "tokens, as you can ",
            "see.",
        ];

        // Act
        var chunks = chunker4.Split(Input, new PlainTextChunkerOptions { MaxTokensPerChunk = 5 });
        DebugChunks(chunks, new FourCharsTestTokenizer());

        // Assert
        Assert.Equal(expected, chunks);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "Chunking")]
    public void CanSplitTextParagraphs()
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
            "This is only a test. We repeat, this is only a test. ",
            "A unit test."
        ];

        // Act
        var chunks = chunker4.Split(string.Join(' ', input), new PlainTextChunkerOptions { MaxTokensPerChunk = 15 });
        DebugChunks(chunks, new FourCharsTestTokenizer());

        // Assert
        Assert.Equal(expected, chunks);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "Chunking")]
    public void CanSplitTextParagraphsEvenly()
    {
        // Arrange
        const char Separator = '\n';
        List<string> input =
        [
            "This is a test of the emergency broadcast system. This is only a test.",
            "We repeat, this is only a test. A unit test.",
            "A small note. And another. And once again. Seriously, this is the end. We're finished. All set. Bye.",
            "Done."
        ];

        string[] expected =
        [
            "This is a test of the emergency broadcast system. ", // 13 tokens
            $"This is only a test.{Separator}We repeat, this is only a test. ", // 14 tokens
            $"A unit test.{Separator}A small note. And another. And once again. ", // 14 tokens
            $"Seriously, this is the end. We're finished. All set. Bye.{Separator}", // 15 tokens
            "Done."
        ];

        // Act
        var chunks = chunker4.Split(string.Join(Separator, input), new PlainTextChunkerOptions { MaxTokensPerChunk = 15 });
        DebugChunks(chunks, new FourCharsTestTokenizer());

        // Assert
        Assert.Equal(expected, chunks);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "Chunking")]
    public void CanSplitTextParagraphsWithHeader()
    {
        // Arrange
        const char Separator = '\n';
        const string ChunkHeader = "DOCUMENT NAME: test.txt\n\n";
        string[] input =
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
        var chunks = chunker4.Split(string.Join(Separator, input), new PlainTextChunkerOptions { MaxTokensPerChunk = 20, ChunkHeader = ChunkHeader });
        DebugChunks(chunks, new FourCharsTestTokenizer());

        // Assert
        Assert.Equal(expected, chunks);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "Chunking")]
    public void CanSplitTextParagraphsWithCustomTokenCounter()
    {
        // Arrange
        const char Separator = '\n';
        List<string> input =
        [
            "This is a test of the emergency broadcast system. This is only a test.",
            "We repeat, this is only a test. A unit test."
        ];

        string[] expected =
        [
            "This is a test of the emergency broadcast system. ",
            $"This is only a test.{Separator}",
            "We repeat, this is only a test. A unit test."
        ];

        // Act
        var chunks = chunker1.Split(string.Join(Separator, input), new PlainTextChunkerOptions { MaxTokensPerChunk = 52 });
        DebugChunks(chunks, new OneCharTestTokenizer());

        // Assert
        Assert.Equal(expected, chunks);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "Chunking")]
    public void CanSplitTextParagraphsWithEmptyInput()
    {
        // Act
        var chunks2 = chunker2.Split("", new PlainTextChunkerOptions { MaxTokensPerChunk = 1 });
        var chunks4 = chunker4.Split("", new PlainTextChunkerOptions { MaxTokensPerChunk = 13 });

        // Assert
        Assert.Empty(chunks2);
        Assert.Empty(chunks4);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "Chunking")]
    public void CanSplitTextParagraphsWithNoDelimiters()
    {
        // Arrange
        const string Separator = "";
        List<string> input =
        [
            "Thisisatestoftheemergencybroadcastsystem",
            "Thisisonlyatest",
            "WerepeatthisisonlyatestAunittest",
            "AsmallnoteAndanotherAndonceagain",
            "SeriouslythisistheendWe'refinishedAllsetByeDoneThisOneWillBeSplitToMeetTheLimit"
        ];

        string[] expected =
        [
            "ThisisatestoftheemergencybroadcastsystemThisisonlyatestWerep", // 15 tokens
            "eatthisisonlyatestAunittestAsmallnoteAndanotherAndonceagainS", // 15 tokens
            "eriouslythisistheendWe'refinishedAllsetByeDoneThisOneWillBeS", // 15 tokens
            "plitToMeetTheLimit" // 5 tokens
        ];

        // Act
        var chunks = chunker4.Split(string.Join(Separator, input), new PlainTextChunkerOptions { MaxTokensPerChunk = 15 });
        DebugChunks(chunks, new FourCharsTestTokenizer());

        // Assert
        Assert.Equal(expected, chunks);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "Chunking")]
    public void CanSplitTextParagraphsSeparatedByNewLine()
    {
        // Arrange
        const string Separator = "\n";
        List<string> input =
        [
            "Thisisatestoftheemergencybroadcastsystem",
            "Thisisonlyatest",
            "WerepeatthisisonlyatestAunittest",
            "AsmallnoteAndanotherAndonceagain",
            "SeriouslythisistheendWe'refinishedAllsetByeDoneThisOneWillBeSplitToMeetTheLimit"
        ];

        string[] expected =
        [
            $"Thisisatestoftheemergencybroadcastsystem{Separator}Thisisonlyatest{Separator}", // 15 tokens
            $"WerepeatthisisonlyatestAunittest{Separator}", // 9 tokens
            $"AsmallnoteAndanotherAndonceagain{Separator}SeriouslythisistheendWe'", // 15 tokens
            "refinishedAllsetByeDoneThisOneWillBeSplitToMeetTheLimit" // 14 tokens
        ];

        // Act
        var chunks = chunker4.Split(string.Join(Separator, input), new PlainTextChunkerOptions { MaxTokensPerChunk = 15 });
        DebugChunks(chunks, new FourCharsTestTokenizer());

        // Assert
        Assert.Equal(expected, chunks);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "Chunking")]
    public void CanSplitTextParagraphsWithHeaderAndCustomTokenCounter()
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
        var chunks = chunker1.Split(string.Join(Separator, input), new PlainTextChunkerOptions { MaxTokensPerChunk = 77, ChunkHeader = ChunkHeader });
        DebugChunks(chunks, new OneCharTestTokenizer());

        // Assert
        Assert.Equal(expected, chunks);
    }

    // a plaintext example that splits on ' '
    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "Chunking")]
    public void CanSplitTextParagraphsOnSpacesV2()
    {
        // Arrange
        const string Separator = "\n";
        List<string> input =
        [
            "This is a test of the emergency broadcast system This is only a test",
            "We repeat this is only a test A unit test",
            "A small note And another And once again Seriously this is the end We're finished All set Bye.",
            "Done."
        ];

        string[] expected =
        [
            "This is a test of the ", // 11 tokens
            "emergency broadcast system ", // 14 tokens
            $"This is only a test{Separator}We repeat ", // 15 tokens
            "this is only a test A unit ", // 14 tokens
            $"test{Separator}A small note And another ", // 15 tokens
            "And once again Seriously this ", // 15 tokens
            "is the end We're finished All ", // 15 tokens
            $"set Bye.{Separator}Done." // 7 tokens
        ];

        // Act
        var chunks = chunker2.Split(string.Join(Separator, input), new PlainTextChunkerOptions { MaxTokensPerChunk = 15 });
        DebugChunks(chunks, new TwoCharsTestTokenizer());

        // Assert
        Assert.Equal(expected, chunks);
    }

    // a plaintext example that splits on ' '
    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "Chunking")]
    public void CanSplitTextParagraphsOnSpacesV4()
    {
        // Arrange
        const string Separator = " ";
        List<string> input =
        [
            "This is a test of the emergency broadcast system This is only a test",
            "We repeat this is only a test A unit test",
            "A small note And another And once again Seriously this is the end We're finished All set Bye.",
            "Done."
        ];

        string[] expected =
        [
            "This is a test of the emergency broadcast system This is ", // 15 tokens
            $"only a test{Separator}We repeat this is only a test A unit test{Separator}A ", // 14 tokens
            "small note And another And once again Seriously this is the ", // 15 tokens
            $"end We're finished All set Bye.{Separator}Done.", // 10 tokens
        ];

        // Act
        var chunks = chunker4.Split(string.Join(Separator, input), new PlainTextChunkerOptions { MaxTokensPerChunk = 15 });
        DebugChunks(chunks, new FourCharsTestTokenizer());

        // Assert
        Assert.Equal(expected, chunks);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "Chunking")]
    public void CanSplitLongStrings()
    {
        // Arrange
        const string Input =
            "this. has. punctuation. like. normal. sentences. " +
            "this is a long sentence without any punctuation or spaces and it is very long and it goes on and on " +
            "thisisalongstringwithoutanyspacesorpunctuationanditisverylonganditgoesonandonandon " +
            "and finally this is a long sentence with punctuation. ";

        string[] expected =
        [
            "this. has. punctuation. like. ", // 9 tokens
            "normal. sentences. ", // 5 tokens
            "this is a long sentence without any punctuation or ", // 10 tokens
            "spaces and it is very long and it goes ", // 10 tokens
            "on and on ", // 4 tokens
            "thisisalongstringwithoutanyspacesorpunct", // 10 tokens
            "uationanditisverylonganditgoesonandon", // 10 tokens
            "andon and finally this is a long sentence with ", // 10 tokens
            "punctuation.", // 3 tokens
        ];

        // Act
        var chunks = new PlainTextChunker(new CL100KTokenizer()).Split(Input, new PlainTextChunkerOptions { MaxTokensPerChunk = 10 });
        DebugChunks(chunks, new CL100KTokenizer());

        // Assert
        Assert.Equal(expected, chunks);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "Chunking")]
    public void SplitsOnSpecialSequencesAndHoldsAllChars()
    {
        // Arrange
        const string Input =
            "Hello!!!It's been a minute!?!Here's a list of numbers: one, two, three, four, five, six, seven, eight, nine, ten⁇" +
            "Hello!!!It's been a minute!?!Here's a list of numbers: one, two, three, four, five, six, seven, eight, nine, ten⁇";

        string[] expected =
        [
            "Hello!!!", // 2 tokens
            "It's been a minute!?!", // 7 tokens
            "Here's a list of numbers: ", // 8 tokens
            "one, two, three, ", // 7 tokens
            "four, five, six, ", // 7 tokens
            "seven, eight, nine, ", // 7 tokens
            "ten⁇Hello!!!", // 5 tokens
            "It's been a minute!?!", // 7 tokens
            "Here's a list of numbers: ", // 8 tokens
            "one, two, three, ", // 7 tokens
            "four, five, six, ", // 7 tokens
            "seven, eight, nine, ", // 7 tokens
            "ten⁇", // 3 tokens
        ];

        // Act
        var chunks = new PlainTextChunker(new CL100KTokenizer()).Split(Input, new PlainTextChunkerOptions { MaxTokensPerChunk = 8 });
        var merged = string.Join("", chunks);
        DebugChunks(chunks, new CL100KTokenizer());

        // Assert
        Assert.Equal(Input, merged);
        Assert.Equal(expected, chunks);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "Chunking")]
    public void CanSplitTextParagraphsWithOverlapAndHeader()
    {
        // Arrange
        const string Separator = "\n";
        const string ChunkHeader = "DOCUMENT NAME: test.txt\n\n";
        string[] input =
        [
            "This is a test of the emergency broadcast system. This is only a test.",
            "We repeat, this is only a test. A unit test."
        ];

        string[] expected =
        [
            $"{ChunkHeader}This is a test of the emergency broadcast system. ", // 19 tokens
            $"{ChunkHeader}e emergency broadcast system. This is only a test.{Separator}", // 19 tokens
            $"{ChunkHeader}This is only a test.{Separator}We repeat, ", // 15 tokens
            $"{ChunkHeader}We repeat, this is only a test. A unit ", // 16 tokens
            $"{ChunkHeader}this is only a test. A unit test.", // 15 tokens
        ];

        // Act
        var chunks = chunker4.Split(string.Join(Separator, input), new PlainTextChunkerOptions { MaxTokensPerChunk = 22, Overlap = 8, ChunkHeader = ChunkHeader });
        DebugChunks(chunks, new FourCharsTestTokenizer());

        // Assert
        Assert.Equal(expected, chunks);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "Chunking")]
    public void CanSplitTextParagraphsWithOverlapAndCustomTokenCounter()
    {
        // Arrange
        const char Separator = '\n';
        string[] input =
        [
            "This is a test of the emergency broadcast system. This is only a test.",
            "We repeat, this is only a test. A unit test."
        ];

        string[] expected =
        [
            $"This is a test of the emergency broadcast system. This is only a test.{Separator}", // 71 tokens
            $" broadcast system. This is only a test.{Separator}We repeat, this is only a test. ", // 72 tokens
            "We repeat, this is only a test. A unit test.", // 44 tokens
        ];

        // Act
        var chunks = chunker1.Split(string.Join(Separator, input), new PlainTextChunkerOptions { MaxTokensPerChunk = 75, Overlap = 40 });
        DebugChunks(chunks, new OneCharTestTokenizer());

        // Assert
        Assert.Equal(expected, chunks);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "Chunking")]
    public void CanSplitTextParagraphsWithOverlap()
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
            "This is a test of the emergency broadcast system. ", // 13 tokens
            $"e emergency broadcast system. This is only a test.{Separator}", // 13 tokens
            $"This is only a test.{Separator}We repeat, ", // 8 tokens
            "We repeat, this is only a test. A unit ", // 10 tokens
            "this is only a test. A unit test.", // 9 tokens
        ];

        // Act
        var chunks = chunker4.Split(string.Join(Separator, input), new PlainTextChunkerOptions { MaxTokensPerChunk = 15, Overlap = 8 });
        DebugChunks(chunks, new FourCharsTestTokenizer());

        // Assert
        Assert.Equal(expected, chunks);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "Chunking")]
    public void CanSplitTextParagraphsOnNewlines()
    {
        // Arrange
        List<string> input =
        [
            "This is a test of the emergency broadcast system\r\n\r\nThis is only a test",
            "We repeat this is only a test\nA unit test",
            "A small note\nAnd another\r\nAnd once again\rSeriously this is the end\n\nWe're finished\nAll set\nBye\n",
            "Done"
        ];

        string[] expected =
        [
            "This is a test of the emergency broadcast system\n\n", // 13 tokens
            "This is only a test\nWe repeat this is only a test\nA unit ", // 15 tokens
            "test\nA small note\nAnd another\nAnd once again\nSeriously this ", // 15 tokens
            "is the end\n\nWe're finished\nAll set\nBye\n\nDone" // 11 tokens
        ];

        // Act
        var chunks = chunker4.Split(string.Join('\n', input), new PlainTextChunkerOptions { MaxTokensPerChunk = 15 });
        DebugChunks(chunks, new FourCharsTestTokenizer());

        // Assert
        Assert.Equal(expected, chunks);
    }

    // a plaintext example that splits on ? or !
    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "Chunking")]
    public void CanSplitTextParagraphsOnPunctuation()
    {
        // Arrange
        const string Separator = "\n";
        List<string> input =
        [
            "This is a test of the emergency broadcast system. This is only a test",
            "We repeat, this is only a test? A unit test",
            "A small note! And another? And once again! Seriously, this is the end. We're finished. All set. Bye.",
            "Done."
        ];

        string[] expected =
        [
            "This is a test of the emergency broadcast system. ", // 13 tokens
            $"This is only a test{Separator}We repeat, this is only a test? ", // 13 tokens
            $"A unit test{Separator}A small note! And another? And once again! ", // 14 tokens
            $"Seriously, this is the end. We're finished. All set. Bye.{Separator}", // 15 tokens
            "Done." // 2 tokens
        ];

        // Act
        var chunks = chunker4.Split(string.Join(Separator, input), new PlainTextChunkerOptions { MaxTokensPerChunk = 15 });

        // Assert
        Assert.Equal(expected, chunks);
    }

    // a plaintext example that splits on ;
    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "Chunking")]
    public void CanSplitTextParagraphsOnSemicolons()
    {
        // Arrange
        const string Separator = "\n";
        List<string> input =
        [
            "This is a test of the emergency broadcast system; This is only a test",
            "We repeat; this is only a test; A unit test",
            "A small note; And another; And once again; Seriously, this is the end; We're finished; All set; Bye.",
            "Done."
        ];

        string[] expected =
        [
            "This is a test of the emergency broadcast system; ",
            $"This is only a test{Separator}We repeat; this is only a test; ",
            $"A unit test{Separator}A small note; And another; And once again; ",
            "Seriously, this is the end; We're finished; All set; ",
            $"Bye.{Separator}Done.",
        ];

        // Act
        var chunks = chunker4.Split(string.Join(Separator, input), new PlainTextChunkerOptions { MaxTokensPerChunk = 15 });

        // Assert
        Assert.Equal(expected, chunks);
    }

    // a plaintext example that splits on :
    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "Chunking")]
    public void CanSplitTextParagraphsOnColons()
    {
        // Arrange
        const string Separator = "\n";
        List<string> input =
        [
            "This is a test of the emergency broadcast system: This is only a test",
            "We repeat: this is only a test: A unit test",
            "A small note: And another: And once again: Seriously, this is the end: We're finished: All set: Bye.",
            "Done."
        ];

        string[] expected =
        [
            "This is a test of the emergency broadcast system: ",
            $"This is only a test{Separator}We repeat: this is only a test: ",
            $"A unit test{Separator}A small note: And another: And once again: ",
            "Seriously, this is the end: We're finished: All set: ",
            $"Bye.{Separator}Done.",
        ];

        // Act
        var chunks = chunker4.Split(string.Join(Separator, input), new PlainTextChunkerOptions { MaxTokensPerChunk = 15 });
        DebugChunks(chunks, new FourCharsTestTokenizer());

        // Assert
        Assert.Equal(expected, chunks);
    }

    // a plaintext example that splits on ,
    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "Chunking")]
    public void CanSplitTextParagraphsOnCommas()
    {
        // Arrange
        const string Separator = "\n";
        List<string> input =
        [
            "This is a test of the emergency broadcast system, This is only a test",
            "We repeat, this is only a test, A unit test",
            "A small note, And another, And once again, Seriously, this is the end, We're finished, All set, Bye.",
            "Done."
        ];

        string[] expected =
        [
            "This is a test of the emergency broadcast system, ",
            $"This is only a test{Separator}We repeat, this is only a test, ",
            $"A unit test{Separator}A small note, And another, And once again, ",
            "Seriously, this is the end, We're finished, All set, ",
            $"Bye.{Separator}Done.",
        ];

        // Act
        var chunks = chunker4.Split(string.Join(Separator, input), new PlainTextChunkerOptions { MaxTokensPerChunk = 15 });
        DebugChunks(chunks, new FourCharsTestTokenizer());

        // Assert
        Assert.Equal(expected, chunks);
    }

    // a plaintext example that splits on ) or ] or }
    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "Chunking")]
    public void CanSplitTextParagraphsOnClosingBrackets()
    {
        // Arrange
        const string Separator = "\n";
        List<string> input =
        [
            "This is a test of the emergency broadcast system) This is only a test",
            "We repeat) this is only a test) A unit test",
            "A small note] And another) And once again] Seriously this is the end} We're finished} All set} Bye.",
            "Done."
        ];

        string[] expected =
        [
            "This is a test of the emergency broadcast system) ",
            $"This is only a test{Separator}We repeat) this is only a test) ",
            $"A unit test{Separator}A small note] And another) And once again] ",
            "Seriously this is the end} We're finished} All set} ",
            $"Bye.{Separator}Done.",
        ];

        // Act
        var chunks = chunker4.Split(string.Join(Separator, input), new PlainTextChunkerOptions { MaxTokensPerChunk = 15 });
        DebugChunks(chunks, new FourCharsTestTokenizer());

        // Assert
        Assert.Equal(expected, chunks);
    }

    // a plaintext example that splits on '-' (very weak separator)
    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "Chunking")]
    public void CanSplitTextParagraphsOnHyphens()
    {
        // Arrange
        const string Separator = "";
        List<string> input =
        [
            "This-is-a-test-of-the-emergency-broadcast-system-This-is-only-a-test",
            "We-repeat-this-is-only-a-test-A-unit-test",
            "A-small-note-And-another-And-once-again-Seriously, this-is-the-end-We're-finished-All-set-Bye.",
            "Done."
        ];

        string[] expected =
        [
            "This-is-a-test-of-the-emergency-broadcast-system-This-is-",
            "only-a-testWe-repeat-this-is-only-a-test-A-unit-testA-small-",
            "note-And-another-And-once-again-Seriously, ",
            "this-is-the-end-We're-finished-All-set-Bye.Done.",
        ];

        // Act
        var chunks = chunker4.Split(string.Join(Separator, input), new PlainTextChunkerOptions { MaxTokensPerChunk = 15 });
        DebugChunks(chunks, new FourCharsTestTokenizer());

        // Assert
        Assert.Equal(expected, chunks);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "Chunking")]
    public void CanSplitTextParagraphsWithOverlapAndHeaderAndCustomTokenCounter()
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
            $"{ChunkHeader}We repeat, this is only a test. A unit test.", // 69 tokens
        ];

        // Act
        var chunks = chunker1.Split(string.Join(Separator, input), new PlainTextChunkerOptions { MaxTokensPerChunk = 100, Overlap = 40, ChunkHeader = ChunkHeader });
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
