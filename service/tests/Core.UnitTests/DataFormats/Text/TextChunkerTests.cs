﻿// Copyright (c) Microsoft. All rights reserved.

using System.Text;
using Microsoft.KernelMemory.DataFormats.Text;

namespace Microsoft.KM.Core.UnitTests.DataFormats.Text;

public sealed class TextChunkerTests
{
    // Use this as the default chunker, to decouple the test from GPT3 tokenizer
    private static readonly TextChunker.TokenCounter s_tokenCounter = s => (s.Length >> 2);

    [Fact]
    [Trait("Category", "UnitTest")]
    public void CanSplitPlainTextLines()
    {
        const string Input = "This is a test of the emergency broadcast system. This is only a test.";
        var expected = new[]
        {
            "This is a test of the emergency broadcast system.",
            "This is only a test."
        };

        var result = TextChunker.SplitPlainTextLines(Input, 15, tokenCounter: s_tokenCounter);

        Assert.Equal(expected, result);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    public void CanSplitMarkdownParagraphs()
    {
        List<string> input = new()
        {
            "This is a test of the emergency broadcast system. This is only a test.",
            "We repeat, this is only a test. A unit test."
        };
        var expected = new[]
        {
            "This is a test of the emergency broadcast system.",
            "This is only a test.",
            "We repeat, this is only a test. A unit test."
        };

        var result = TextChunker.SplitMarkdownParagraphs(input, 13, tokenCounter: s_tokenCounter);

        Assert.Equal(expected, result);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    public void CanSplitMarkdownParagraphsWithOverlap()
    {
        List<string> input = new()
        {
            "This is a test of the emergency broadcast system. This is only a test.",
            "We repeat, this is only a test. A unit test."
        };

        var expected = new[]
        {
            "This is a test of the emergency broadcast system.",
            "emergency broadcast system. This is only a test.",
            "This is only a test. We repeat, this is only a test.",
            "We repeat, this is only a test. A unit test.",
            "A unit test."
        };

        var result = TextChunker.SplitMarkdownParagraphs(input, 15, 8, tokenCounter: s_tokenCounter);

        Assert.Equal(expected, result);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    public void CanSplitTextParagraphs()
    {
        List<string> input = new()
        {
            "This is a test of the emergency broadcast system. This is only a test.",
            "We repeat, this is only a test. A unit test."
        };

        var expected = new[]
        {
            "This is a test of the emergency broadcast system.",
            "This is only a test.",
            "We repeat, this is only a test. A unit test."
        };

        var result = TextChunker.SplitPlainTextParagraphs(input, 13, tokenCounter: s_tokenCounter);

        Assert.Equal(expected, result);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    public void CanSplitTextParagraphsWithOverlap()
    {
        List<string> input = new()
        {
            "This is a test of the emergency broadcast system. This is only a test.",
            "We repeat, this is only a test. A unit test."
        };

        var expected = new[]
        {
            "This is a test of the emergency broadcast system.",
            "emergency broadcast system. This is only a test.",
            "This is only a test. We repeat, this is only a test.",
            "We repeat, this is only a test. A unit test.",
            "A unit test."
        };

        var result = TextChunker.SplitPlainTextParagraphs(input, 15, 8, tokenCounter: s_tokenCounter);

        Assert.Equal(expected, result);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    public void CanSplitMarkDownLines()
    {
        const string Input = "This is a test of the emergency broadcast system. This is only a test.";
        var expected = new[]
        {
            "This is a test of the emergency broadcast system.",
            "This is only a test."
        };

        var result = TextChunker.SplitMarkDownLines(Input, 15, tokenCounter: s_tokenCounter);

        Assert.Equal(expected, result);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    public void CanSplitTextParagraphsWithEmptyInput()
    {
        List<string> input = new();

        var expected = new List<string>();

        var result = TextChunker.SplitPlainTextParagraphs(input, 13, tokenCounter: s_tokenCounter);

        Assert.Equal(expected, result);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    public void CanSplitMarkdownParagraphsWithEmptyInput()
    {
        List<string> input = new();

        var expected = new List<string>();

        var result = TextChunker.SplitMarkdownParagraphs(input, 13, tokenCounter: s_tokenCounter);

        Assert.Equal(expected, result);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    public void CanSplitTextParagraphsEvenly()
    {
        List<string> input = new()
        {
            "This is a test of the emergency broadcast system. This is only a test.",
            "We repeat, this is only a test. A unit test.",
            "A small note. And another. And once again. Seriously, this is the end. We're finished. All set. Bye.",
            "Done."
        };

        var expected = new[]
        {
            "This is a test of the emergency broadcast system.",
            "This is only a test.",
            "We repeat, this is only a test. A unit test.",
            "A small note. And another. And once again.",
            "Seriously, this is the end. We're finished. All set. Bye. Done."
        };

        var result = TextChunker.SplitPlainTextParagraphs(input, 15, tokenCounter: s_tokenCounter);

        Assert.Equal(expected, result);
    }

    // a plaintext example that splits on \r or \n
    [Fact]
    [Trait("Category", "UnitTest")]
    public void CanSplitTextParagraphsOnNewlines()
    {
        List<string> input = new()
        {
            "This is a test of the emergency broadcast system\r\nThis is only a test",
            "We repeat this is only a test\nA unit test",
            "A small note\nAnd another\r\nAnd once again\rSeriously this is the end\nWe're finished\nAll set\nBye\n",
            "Done"
        };

        var expected = new[]
        {
            "This is a test of the emergency broadcast system",
            "This is only a test",
            "We repeat this is only a test\nA unit test",
            "A small note\nAnd another\nAnd once again",
            "Seriously this is the end\nWe're finished\nAll set\nBye Done",
        };

        var result = TextChunker.SplitPlainTextParagraphs(input, 15, tokenCounter: s_tokenCounter);

        Assert.Equal(expected, result);
    }

    // a plaintext example that splits on ? or !
    [Fact]
    [Trait("Category", "UnitTest")]
    public void CanSplitTextParagraphsOnPunctuation()
    {
        List<string> input = new()
        {
            "This is a test of the emergency broadcast system. This is only a test",
            "We repeat, this is only a test? A unit test",
            "A small note! And another? And once again! Seriously, this is the end. We're finished. All set. Bye.",
            "Done."
        };

        var expected = new[]
        {
            "This is a test of the emergency broadcast system.",
            "This is only a test",
            "We repeat, this is only a test? A unit test",
            "A small note! And another? And once again!",
            "Seriously, this is the end.",
            $"We're finished. All set. Bye.{Environment.NewLine}Done.",
        };

        var result = TextChunker.SplitPlainTextParagraphs(input, 15, tokenCounter: s_tokenCounter);

        Assert.Equal(expected, result);
    }

    // a plaintext example that splits on ;
    [Fact]
    [Trait("Category", "UnitTest")]
    public void CanSplitTextParagraphsOnSemicolons()
    {
        List<string> input = new()
        {
            "This is a test of the emergency broadcast system; This is only a test",
            "We repeat; this is only a test; A unit test",
            "A small note; And another; And once again; Seriously, this is the end; We're finished; All set; Bye.",
            "Done."
        };

        var expected = new[]
        {
            "This is a test of the emergency broadcast system;",
            "This is only a test",
            "We repeat; this is only a test; A unit test",
            "A small note; And another; And once again;",
            "Seriously, this is the end; We're finished; All set; Bye. Done.",
        };

        var result = TextChunker.SplitPlainTextParagraphs(input, 15, tokenCounter: s_tokenCounter);

        Assert.Equal(expected, result);
    }

    // a plaintext example that splits on :
    [Fact]
    [Trait("Category", "UnitTest")]
    public void CanSplitTextParagraphsOnColons()
    {
        List<string> input = new()
        {
            "This is a test of the emergency broadcast system: This is only a test",
            "We repeat: this is only a test: A unit test",
            "A small note: And another: And once again: Seriously, this is the end: We're finished: All set: Bye.",
            "Done."
        };

        var expected = new[]
        {
            "This is a test of the emergency broadcast system:",
            "This is only a test",
            "We repeat: this is only a test: A unit test",
            "A small note: And another: And once again:",
            "Seriously, this is the end: We're finished: All set: Bye. Done.",
        };

        var result = TextChunker.SplitPlainTextParagraphs(input, 15, tokenCounter: s_tokenCounter);

        Assert.Equal(expected, result);
    }

    // a plaintext example that splits on ,
    [Fact]
    [Trait("Category", "UnitTest")]
    public void CanSplitTextParagraphsOnCommas()
    {
        List<string> input = new()
        {
            "This is a test of the emergency broadcast system, This is only a test",
            "We repeat, this is only a test, A unit test",
            "A small note, And another, And once again, Seriously, this is the end, We're finished, All set, Bye.",
            "Done."
        };

        var expected = new[]
        {
            "This is a test of the emergency broadcast system,",
            "This is only a test",
            "We repeat, this is only a test, A unit test",
            "A small note, And another, And once again, Seriously,",
            $"this is the end, We're finished, All set, Bye.{Environment.NewLine}Done.",
        };

        var result = TextChunker.SplitPlainTextParagraphs(input, 15, tokenCounter: s_tokenCounter);

        Assert.Equal(expected, result);
    }

    // a plaintext example that splits on ) or ] or }
    [Fact]
    [Trait("Category", "UnitTest")]
    public void CanSplitTextParagraphsOnClosingBrackets()
    {
        List<string> input = new()
        {
            "This is a test of the emergency broadcast system) This is only a test",
            "We repeat) this is only a test) A unit test",
            "A small note] And another) And once again] Seriously this is the end} We're finished} All set} Bye.",
            "Done."
        };

        var expected = new[]
        {
            "This is a test of the emergency broadcast system)",
            "This is only a test",
            "We repeat) this is only a test) A unit test",
            "A small note] And another) And once again]",
            "Seriously this is the end} We're finished} All set} Bye. Done.",
        };

        var result = TextChunker.SplitPlainTextParagraphs(input, 15, tokenCounter: s_tokenCounter);

        Assert.Equal(expected, result);
    }

    // a plaintext example that splits on ' '
    [Fact]
    [Trait("Category", "UnitTest")]
    public void CanSplitTextParagraphsOnSpaces()
    {
        List<string> input = new()
        {
            "This is a test of the emergency broadcast system This is only a test",
            "We repeat this is only a test A unit test",
            "A small note And another And once again Seriously this is the end We're finished All set Bye.",
            "Done."
        };

        var expected = new[]
        {
            "This is a test of the emergency",
            "broadcast system This is only a test",
            "We repeat this is only a test A unit test",
            "A small note And another And once again Seriously",
            $"this is the end We're finished All set Bye.{Environment.NewLine}Done.",
        };

        var result = TextChunker.SplitPlainTextParagraphs(input, 15, tokenCounter: s_tokenCounter);

        Assert.Equal(expected, result);
    }

    // a plaintext example that splits on '-'
    [Fact]
    [Trait("Category", "UnitTest")]
    public void CanSplitTextParagraphsOnHyphens()
    {
        List<string> input = new()
        {
            "This is a test of the emergency broadcast system-This is only a test",
            "We repeat-this is only a test-A unit test",
            "A small note-And another-And once again-Seriously, this is the end-We're finished-All set-Bye.",
            "Done."
        };

        var expected = new[]
        {
            "This is a test of the emergency",
            "broadcast system-This is only a test",
            "We repeat-this is only a test-A unit test",
            "A small note-And another-And once again-Seriously,",
            $"this is the end-We're finished-All set-Bye.{Environment.NewLine}Done.",
        };

        var result = TextChunker.SplitPlainTextParagraphs(input, 15, tokenCounter: s_tokenCounter);

        Assert.Equal(expected, result);
    }

    // a plaintext example that does not have any of the above characters
    [Fact]
    [Trait("Category", "UnitTest")]
    public void CanSplitTextParagraphsWithNoDelimiters()
    {
        List<string> input = new()
        {
            "Thisisatestoftheemergencybroadcastsystem",
            "Thisisonlyatest",
            "WerepeatthisisonlyatestAunittest",
            "AsmallnoteAndanotherAndonceagain",
            "SeriouslythisistheendWe'refinishedAllsetByeDoneThisOneWillBeSplitToMeetTheLimit",
        };

        var expected = new[]
        {
            $"Thisisatestoftheemergencybroadcastsystem{Environment.NewLine}Thisisonlyatest",
            "WerepeatthisisonlyatestAunittest",
            "AsmallnoteAndanotherAndonceagain",
            "SeriouslythisistheendWe'refinishedAllse",
            "tByeDoneThisOneWillBeSplitToMeetTheLimit",
        };

        var result = TextChunker.SplitPlainTextParagraphs(input, 15, tokenCounter: s_tokenCounter);

        Assert.Equal(expected, result);
    }

    // a markdown example that splits on .

    // a markdown example that splits on ? or !

    // a markdown example that splits on ;

    // a markdown example that splits on :

    // a markdown example that splits on ,

    // a markdown example that splits on ) or ] or }

    // a markdown example that splits on ' '

    // a markdown example that splits on '-'

    // a markdown example that splits on '\r' or '\n'
    [Fact]
    [Trait("Category", "UnitTest")]
    public void CanSplitMarkdownParagraphsOnNewlines()
    {
        List<string> input = new()
        {
            "This_is_a_test_of_the_emergency_broadcast_system\r\nThis_is_only_a_test",
            "We_repeat_this_is_only_a_test\nA_unit_test",
            "A_small_note\nAnd_another\r\nAnd_once_again\rSeriously_this_is_the_end\nWe're_finished\nAll_set\nBye\n",
            "Done"
        };

        var expected = new[]
        {
            "This_is_a_test_of_the_emergency_broadcast_system",
            "This_is_only_a_test",
            "We_repeat_this_is_only_a_test\nA_unit_test",
            "A_small_note\nAnd_another\nAnd_once_again",
            "Seriously_this_is_the_end\nWe're_finished\nAll_set\nBye Done",
        };

        var result = TextChunker.SplitMarkdownParagraphs(input, 15, tokenCounter: s_tokenCounter);

        Assert.Equal(expected, result);
    }

    // a markdown example that does not have any of the above characters
    [Fact]
    [Trait("Category", "UnitTest")]
    public void CanSplitVeryLargeDocumentsWithoutStackOverflowing()
    {
#pragma warning disable CA5394 // this test relies on repeatable pseudo-random numbers
        var rand = new Random(42);
        var sb = new StringBuilder(100_000 * 11);
        for (int wordNum = 0; wordNum < 100_000; wordNum++)
        {
            int wordLength = rand.Next(1, 10);
            for (int charNum = 0; charNum < wordLength; charNum++)
            {
                sb.Append((char)('a' + rand.Next(0, 26)));
            }

            sb.Append(' ');
        }

        string text = sb.ToString();
        List<string> lines = TextChunker.SplitPlainTextLines(text, 20, tokenCounter: s_tokenCounter);
        List<string> paragraphs = TextChunker.SplitPlainTextParagraphs(lines, 200, tokenCounter: s_tokenCounter);
        Assert.NotEmpty(paragraphs);
#pragma warning restore CA5394
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    public void CanSplitPlainTextLinesWithCustomTokenCounter()
    {
        const string Input = "This is a test of the emergency broadcast system. This is only a test.";
        var expected = new[]
        {
            "This is a test of the emergency broadcast system.",
            "This is only a test."
        };

        var result = TextChunker.SplitPlainTextLines(Input, 60, s => s.Length);

        Assert.Equal(expected, result);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    public void CanSplitMarkdownParagraphsWithCustomTokenCounter()
    {
        List<string> input = new()
        {
            "This is a test of the emergency broadcast system. This is only a test.",
            "We repeat, this is only a test. A unit test."
        };
        var expected = new[]
        {
            "This is a test of the emergency broadcast system.",
            "This is only a test.",
            "We repeat, this is only a test. A unit test."
        };

        var result = TextChunker.SplitMarkdownParagraphs(input, 52, tokenCounter: s => s.Length);

        Assert.Equal(expected, result);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    public void CanSplitMarkdownParagraphsWithOverlapAndCustomTokenCounter()
    {
        List<string> input = new()
        {
            "This is a test of the emergency broadcast system. This is only a test.",
            "We repeat, this is only a test. A unit test."
        };

        var expected = new[]
        {
            "This is a test of the emergency broadcast system.",
            "emergency broadcast system. This is only a test.",
            "This is only a test. We repeat, this is only a test.",
            "We repeat, this is only a test. A unit test.",
            "A unit test."
        };

        var result = TextChunker.SplitMarkdownParagraphs(input, 75, 40, tokenCounter: s => s.Length);

        Assert.Equal(expected, result);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    public void CanSplitTextParagraphsWithCustomTokenCounter()
    {
        List<string> input = new()
        {
            "This is a test of the emergency broadcast system. This is only a test.",
            "We repeat, this is only a test. A unit test."
        };

        var expected = new[]
        {
            "This is a test of the emergency broadcast system.",
            "This is only a test.",
            "We repeat, this is only a test. A unit test."
        };

        var result = TextChunker.SplitPlainTextParagraphs(input, 52, tokenCounter: s => s.Length);

        Assert.Equal(expected, result);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    public void CanSplitTextParagraphsWithOverlapAndCustomTokenCounter()
    {
        List<string> input = new()
        {
            "This is a test of the emergency broadcast system. This is only a test.",
            "We repeat, this is only a test. A unit test."
        };

        var expected = new[]
        {
            "This is a test of the emergency broadcast system.",
            "emergency broadcast system. This is only a test.",
            "This is only a test. We repeat, this is only a test.",
            "We repeat, this is only a test. A unit test.",
            "A unit test."
        };

        var result = TextChunker.SplitPlainTextParagraphs(input, 75, 40, tokenCounter: s => s.Length);

        Assert.Equal(expected, result);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    public void CanSplitMarkDownLinesWithCustomTokenCounter()
    {
        const string Input = "This is a test of the emergency broadcast system. This is only a test.";
        var expected = new[]
        {
            "This is a test of the emergency broadcast system.",
            "This is only a test."
        };

        var result = TextChunker.SplitMarkDownLines(Input, 60, tokenCounter: s => s.Length);

        Assert.Equal(expected, result);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    public void CanSplitMarkdownParagraphsWithHeader()
    {
        const string ChunkHeader = "DOCUMENT NAME: test.txt\n\n";
        List<string> input = new()
        {
            "This is a test of the emergency broadcast system. This is only a test.",
            "We repeat, this is only a test. A unit test."
        };
        var expected = new[]
        {
            $"{ChunkHeader}This is a test of the emergency broadcast system.",
            $"{ChunkHeader}This is only a test.",
            $"{ChunkHeader}We repeat, this is only a test. A unit test."
        };

        var result = TextChunker.SplitMarkdownParagraphs(input, 20, chunkHeader: ChunkHeader, tokenCounter: s_tokenCounter);

        Assert.Equal(expected, result);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    public void CanSplitMarkdownParagraphsWithOverlapAndHeader()
    {
        const string ChunkHeader = "DOCUMENT NAME: test.txt\n\n";
        List<string> input = new()
        {
            "This is a test of the emergency broadcast system. This is only a test.",
            "We repeat, this is only a test. A unit test."
        };

        var expected = new[]
        {
            $"{ChunkHeader}This is a test of the emergency broadcast system.",
            $"{ChunkHeader}emergency broadcast system. This is only a test.",
            $"{ChunkHeader}This is only a test. We repeat, this is only a test.",
            $"{ChunkHeader}We repeat, this is only a test. A unit test.",
            $"{ChunkHeader}A unit test."
        };

        var result = TextChunker.SplitMarkdownParagraphs(input, 22, 8, chunkHeader: ChunkHeader, tokenCounter: s_tokenCounter);

        Assert.Equal(expected, result);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    public void CanSplitTextParagraphsWithHeader()
    {
        const string ChunkHeader = "DOCUMENT NAME: test.txt\n\n";
        List<string> input = new()
        {
            "This is a test of the emergency broadcast system. This is only a test.",
            "We repeat, this is only a test. A unit test."
        };

        var expected = new[]
        {
            $"{ChunkHeader}This is a test of the emergency broadcast system.",
            $"{ChunkHeader}This is only a test.",
            $"{ChunkHeader}We repeat, this is only a test. A unit test."
        };

        var result = TextChunker.SplitPlainTextParagraphs(input, 20, chunkHeader: ChunkHeader, tokenCounter: s_tokenCounter);

        Assert.Equal(expected, result);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    public void CanSplitTextParagraphsWithOverlapAndHeader()
    {
        const string ChunkHeader = "DOCUMENT NAME: test.txt\n\n";
        List<string> input = new()
        {
            "This is a test of the emergency broadcast system. This is only a test.",
            "We repeat, this is only a test. A unit test."
        };

        var expected = new[]
        {
            $"{ChunkHeader}This is a test of the emergency broadcast system.",
            $"{ChunkHeader}emergency broadcast system. This is only a test.",
            $"{ChunkHeader}This is only a test. We repeat, this is only a test.",
            $"{ChunkHeader}We repeat, this is only a test. A unit test.",
            $"{ChunkHeader}A unit test."
        };

        var result = TextChunker.SplitPlainTextParagraphs(input, 22, 8, chunkHeader: ChunkHeader, tokenCounter: s_tokenCounter);

        Assert.Equal(expected, result);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    public void CanSplitMarkdownParagraphsWithHeaderAndCustomTokenCounter()
    {
        const string ChunkHeader = "DOCUMENT NAME: test.txt\n\n";
        List<string> input = new()
        {
            "This is a test of the emergency broadcast system. This is only a test.",
            "We repeat, this is only a test. A unit test."
        };
        var expected = new[]
        {
            $"{ChunkHeader}This is a test of the emergency broadcast system.",
            $"{ChunkHeader}This is only a test.",
            $"{ChunkHeader}We repeat, this is only a test. A unit test."
        };

        var result = TextChunker.SplitMarkdownParagraphs(input, 77, chunkHeader: ChunkHeader, tokenCounter: s => s.Length);

        Assert.Equal(expected, result);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    public void CanSplitMarkdownParagraphsWithOverlapAndHeaderAndCustomTokenCounter()
    {
        const string ChunkHeader = "DOCUMENT NAME: test.txt\n\n";
        List<string> input = new()
        {
            "This is a test of the emergency broadcast system. This is only a test.",
            "We repeat, this is only a test. A unit test."
        };

        var expected = new[]
        {
            $"{ChunkHeader}This is a test of the emergency broadcast system.",
            $"{ChunkHeader}emergency broadcast system. This is only a test.",
            $"{ChunkHeader}This is only a test. We repeat, this is only a test.",
            $"{ChunkHeader}We repeat, this is only a test. A unit test.",
            $"{ChunkHeader}A unit test."
        };

        var result = TextChunker.SplitMarkdownParagraphs(input, 100, 40, chunkHeader: ChunkHeader, tokenCounter: s => s.Length);

        Assert.Equal(expected, result);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    public void CanSplitTextParagraphsWithHeaderAndCustomTokenCounter()
    {
        const string ChunkHeader = "DOCUMENT NAME: test.txt\n\n";
        List<string> input = new()
        {
            "This is a test of the emergency broadcast system. This is only a test.",
            "We repeat, this is only a test. A unit test."
        };

        var expected = new[]
        {
            $"{ChunkHeader}This is a test of the emergency broadcast system.",
            $"{ChunkHeader}This is only a test.",
            $"{ChunkHeader}We repeat, this is only a test. A unit test."
        };

        var result = TextChunker.SplitPlainTextParagraphs(input, 77, chunkHeader: ChunkHeader, tokenCounter: s => s.Length);

        Assert.Equal(expected, result);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    public void CanSplitTextParagraphsWithOverlapAndHeaderAndCustomTokenCounter()
    {
        const string ChunkHeader = "DOCUMENT NAME: test.txt\n\n";
        List<string> input = new()
        {
            "This is a test of the emergency broadcast system. This is only a test.",
            "We repeat, this is only a test. A unit test."
        };

        var expected = new[]
        {
            $"{ChunkHeader}This is a test of the emergency broadcast system.",
            $"{ChunkHeader}emergency broadcast system. This is only a test.",
            $"{ChunkHeader}This is only a test. We repeat, this is only a test.",
            $"{ChunkHeader}We repeat, this is only a test. A unit test.",
            $"{ChunkHeader}A unit test."
        };

        var result = TextChunker.SplitPlainTextParagraphs(input, 100, 40, chunkHeader: ChunkHeader, tokenCounter: s => s.Length);

        Assert.Equal(expected, result);
    }
}
