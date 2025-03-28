// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.Chunkers;
using Microsoft.KM.TestHelpers;

namespace Microsoft.Chunkers.UnitTests;

public class MarkDownChunkerManualTest(ITestOutputHelper output) : BaseUnitTestCase(output)
{
    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "Chunking")]
    [Trait("Category", "Manual")]
    public void ItSplitsMarkdownInASensibleWay()
    {
        // Arrange
        string text = File.ReadAllText("doc2.md");
        text = $"{text}{text}";

        // Act
        var w = new Stopwatch();
        w.Start();
        var chunks = new MarkDownChunker(new CL100KTokenizer()).Split(text, new MarkDownChunkerOptions { MaxTokensPerChunk = 600, Overlap = 60 });
        w.Stop();

        Console.WriteLine($"Text length: {text.Length:N0} chars");
        Console.WriteLine($"Chunks: {chunks.Count}");
        Console.WriteLine($"Time: {w.ElapsedMilliseconds:N0} ms");

        // Assert
        Assert.NotEmpty(chunks);
        DebugChunks(chunks, new CL100KTokenizer());
    }

    private static void DebugChunks(IEnumerable<string> chunks, ITextTokenizer tokenizer)
    {
        var list = chunks.ToList();

        for (int index = 0; index < list.Count; index++)
        {
            Console.WriteLine($"************************* {index}: [{tokenizer.CountTokens(list[index])} tokens] *****************************************");
            Console.WriteLine(list[index]);
            Console.WriteLine("***********************************************************************************");
        }
    }
}
