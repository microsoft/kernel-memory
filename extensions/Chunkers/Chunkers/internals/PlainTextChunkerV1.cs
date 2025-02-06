// Copyright (c) Microsoft. All rights reserved.

// #define DEBUGCHUNKS__
// #define DEBUGFRAGMENTS__
//
// using System;
// using System.Collections.Generic;
// using System.Diagnostics.CodeAnalysis;
// using System.Linq;
// using System.Text;
// using Microsoft.KernelMemory.AI;
//
// namespace Microsoft.KernelMemory.Chunkers.internals;
//
// /// <summary>
// /// Plain text chunker for splitting text into blocks of a maximum number of tokens.
// /// Designed for Plain Text and RAG scenarios, where some special chars are irrelevant
// /// and can be removed, ie. the split can be lossy.
// /// This chunker should not be used for MarkDown, where symbols have a special meaning,
// /// or different priorities for splitting.
// /// Although not designed to chunk source code or math formulas, it tries to do its best.
// /// Acronyms with dots (e.g. N.A.S.A.) are not considered and are potentially split like sentences.
// /// Anomalous-long sentences are split during the chunking loop, potentially introducing noise at the start of following chunks.
// /// TODO: improve performance
// /// </summary>
// [Experimental("KMEXP00")]
// public class PlainTextChunkerV1
// {
//     public class Options
//     {
//         /// <summary>
//         /// Maximum number of tokens per chunk
//         /// </summary>
//         public int MaxTokensPerChunk { get; set; } = 1024;
//
//         /// <summary>
//         /// Number of tokens to copy and repeat from a chunk into the next.
//         /// </summary>
//         public int Overlap { get; set; } = 0;
//
//         /// <summary>
//         /// Optional header to add before each chunk.
//         /// </summary>
//         public string? ChunkHeader { get; set; } = null;
//     }
//
//     internal enum SeparatorTypes
//     {
//         NotASeparator = 0,
//         ExplicitSeparator = 1,
//         PotentialSeparator = 2,
//         WeakSeparator = 3,
//     }
//
//     internal class Fragment
//     {
//         internal string Content = string.Empty;
//         internal SeparatorTypes SeparatorType = SeparatorTypes.NotASeparator;
//     }
//
//     private class ChunkBuilder
//     {
//         public readonly StringBuilder FullContent = new();
//         public readonly StringBuilder NextSentence = new();
//     }
//
//     // Do not allow chunks smaller than this size, to avoid unnecessary computation.
//     // Realistically, a chunk should be at least 1000 tokens long.
//     private const int MinChunkSize = 5;
//
//     private readonly ITextTokenizer _tokenizer;
//
//     // Prioritized list of characters to split sentence from sentence.
//     private static readonly List<string> s_explicitSplitSequences =
//     [
//         // Symbol + space
//         ". ", ".\t", ".\n", // note: covers also the case of multiple '.' like "....\n"
//         "? ", "?\t", "?\n", // note: covers also the case of multiple '?' and '!?' like "?????\n" and "?!?\n"
//         "! ", "!\t", "!\n", // note: covers also the case of multiple '!' and '?!' like "!!!\n" and "!?!\n"
//         "⁉ ", "⁉\t", "⁉\n",
//         "⁈ ", "⁈\t", "⁈\n",
//         "⁇ ", "⁇\t", "⁇\n",
//         "… ", "…\t", "…\n",
//         // Multi-char separators without space, ordered by length
//         "!!!!", "????", "!!!", "???", "?!?", "!?!", "!?", "?!", "!!", "??", "....", "...", "..",
//         // 1 char separators without space
//         ".", "?", "!", "⁉", "⁈", "⁇", "…",
//     ];
//
//     // Prioritized list of characters to split inside a sentence.
//     private static readonly List<string> s_potentialSplitSequences =
//     [
//         "; ", ";\t", ";\n", ";",
//         "} ", "}\t", "}\n", "}", // note: curly brace without spaces is up here because it's a common code ending char, more important than ')' or ']'
//         ") ", ")\t", ")\n",
//         "] ", "]\t", "]\n",
//         ")", "]",
//     ];
//
//     // Prioritized list of characters to split inside a sentence when other splits are not found.
//     private static readonly List<string> s_weakSplitSequences =
//     [
//         ":", // note: \n \t make no difference with this char
//         ",", // note: \n \t make no difference with this char
//         " ", // note: \n \t make no difference with this char
//         "-", // note: \n \t make no difference with this char
//     ];
//
//     public PlainTextChunkerV1(ITextTokenizer? tokenizer = null)
//     {
//         this._tokenizer = tokenizer ?? new CL100KTokenizer();
//
//         // Check that split options are shorter than 5 chars
//         if (s_explicitSplitSequences.Any(x => x is { Length: > 4 }))
//         {
//             throw new SystemException(nameof(PlainTextChunkerV1) + " contains invalid split sequences, max four chars sequences are supported.");
//         }
//
//         if (s_potentialSplitSequences.Any(x => x is { Length: > 4 }))
//         {
//             throw new SystemException(nameof(PlainTextChunkerV1) + " contains invalid split sequences, max four chars sequences are supported.");
//         }
//
//         if (s_weakSplitSequences.Any(x => x is { Length: > 4 }))
//         {
//             throw new SystemException(nameof(PlainTextChunkerV1) + " contains invalid split sequences, max four chars sequences are supported.");
//         }
//     }
//
//     /// <summary>
//     /// Split plain text into blocks.
//     /// Note:
//     /// - \r\n characters are replaced with \n
//     /// - \r characters are replaced with \n
//     /// - \t character is not replaced
//     /// - Chunks cannot be smaller than [MinChunkSize] tokens (header excluded)
//     /// </summary>
//     /// <param name="text">Text to split</param>
//     /// <param name="options">How to handle input and how to generate chunks</param>
//     /// <returns>List of chunks.</returns>
//     public List<string> Split(string text, Options options)
//     {
//         ArgumentNullException.ThrowIfNull(text);
//         ArgumentNullException.ThrowIfNull(options);
//
//         // Clean up text. Note: LLMs don't use \r char
//         text = text
//             .Replace("\r\n", "\n", StringComparison.OrdinalIgnoreCase)
//             .Replace("\r", "\n", StringComparison.OrdinalIgnoreCase)
//             .Trim();
//
//         // Calculate chunk size leaving room for the optional chunk header
//         int maxChunkSize = Math.Max(MinChunkSize, options.MaxTokensPerChunk - this.TokenCount(options.ChunkHeader));
//
//         // Chunk using recursive logic, starting with explicit separators and moving to weaker ones if needed
//         var chunks = this.RecursiveSplit(text, maxChunkSize, SeparatorTypes.ExplicitSeparator);
//
//         // Add header to each chunk
//         if (!string.IsNullOrEmpty(options.ChunkHeader))
//         {
//             chunks = chunks.Select(x => $"{options.ChunkHeader}{x}").ToList();
//         }
//
//         // TODO: add overlapping tokens
//
// #if DEBUGCHUNKS
//         this.DebugChunks(chunks);
// #endif
//
//         return chunks;
//     }
//
//     /// <summary>
//     /// Greedy algorithm aggregating fragments into chunks separated by a specific separator type.
//     /// If any of the generated chunks is too long, those are split recursively using weaker separators.
//     /// </summary>
//     /// <param name="text">Text to split</param>
//     /// <param name="maxChunkSize">Max size of each chunk</param>
//     /// <param name="separatorType">Type of separator to detect</param>
//     /// <returns>List of strings</returns>
//     internal List<string> RecursiveSplit(string text, int maxChunkSize, SeparatorTypes separatorType)
//     {
//         // Edge case: empty text
//         if (string.IsNullOrEmpty(text)) { return []; }
//
//         // Edge case: text is already short enough
//         if (this.TokenCount(text) <= maxChunkSize) { return [text]; }
//
//         // Important: 'SplitToFragments' splits content in words and delimiters, using logic specific to plain text.
//         //            These are different from LLM tokens, which are based on the tokenizer used to train the model.
//         // Recursive logic exit clause: when separator type is NotASeparator, count each char as a fragment
//         // TODO: reuse fragments from previous calls, this call is very expensive
//         List<Fragment> fragments = separatorType != SeparatorTypes.NotASeparator
//             ? SplitToFragments(text)
//             : text.Select(x => new Fragment { Content = x.ToString(), SeparatorType = SeparatorTypes.NotASeparator }).ToList();
//
//         var chunks = this.GenerateChunks(fragments, maxChunkSize, separatorType);
//
//         // TODO: overlap
//
//         return chunks;
//     }
//
//     internal static SeparatorTypes NextSeparatorType(SeparatorTypes separatorType)
//     {
//         switch (separatorType)
//         {
//             case SeparatorTypes.ExplicitSeparator: return SeparatorTypes.PotentialSeparator;
//             case SeparatorTypes.PotentialSeparator: return SeparatorTypes.WeakSeparator;
//             case SeparatorTypes.WeakSeparator: return SeparatorTypes.NotASeparator;
//             default: throw new ArgumentOutOfRangeException(nameof(SeparatorTypes.NotASeparator) + " doesn't have a next separator type.");
//         }
//     }
//
//     internal List<string> GenerateChunks(List<Fragment> fragments, int maxChunkSize, SeparatorTypes separatorType)
//     {
//         if (fragments.Count == 0) { return []; }
//
//         var chunks = new List<string>();
//         var chunk = new ChunkBuilder();
//
//         foreach (var fragment in fragments)
//         {
//             // Note: fragments != LLM tokens. One fragment can contain multiple tokens.
//             chunk.NextSentence.Append(fragment.Content);
//
//             // PERFORMANCE: wait for a complete sentence, avoiding expensive string computations
//             if (fragment.SeparatorType != separatorType) { continue; }
//
//             var nextSentence = chunk.NextSentence.ToString();
//             var nextSentenceSize = this.TokenCount(nextSentence);
//
//             // Detect current state
//             // 1:
//             // - the current chunk is still empty
//             // - the next sentence is complete and is NOT too long
//             // 2:
//             // - the current chunk is still empty
//             // - the next sentence is complete and is TOO LONG
//             // 3:
//             // - the current chunk is NOT empty
//             // - the next sentence is complete and is NOT too long
//             // 4:
//             // - the current chunk is NOT empty
//             // - the next sentence is complete and is TOO LONG
//             int state;
//             if (chunk.FullContent.Length == 0)
//             {
//                 state = (nextSentenceSize <= maxChunkSize) ? 1 : 2;
//             }
//             else
//             {
//                 state = (nextSentenceSize <= maxChunkSize) ? 3 : 4;
//             }
//
//             switch (state)
//             {
//                 default:
//                     throw new ArgumentOutOfRangeException(nameof(state));
//
//                 // - the current chunk is still empty
//                 // - the next sentence is complete and is NOT too long
//                 case 1:
//                     chunk.FullContent.Append(nextSentence);
//                     chunk.NextSentence.Clear();
//                     continue;
//
//                 // - the current chunk is still empty
//                 // - the next sentence is complete and is TOO LONG
//                 case 2:
//                 {
//                     var moreChunks = this.RecursiveSplit(nextSentence, maxChunkSize, NextSeparatorType(separatorType));
//                     chunks.AddRange(moreChunks.Take(moreChunks.Count - 1));
//                     chunk.NextSentence.Clear().Append(moreChunks.Last());
//                     continue;
//                 }
//
//                 // - the current chunk is NOT empty
//                 // - the next sentence is complete and is NOT too long
//                 case 3:
//                 {
//                     var chunkPlusSentence = $"{chunk.FullContent}{chunk.NextSentence}";
//                     if (this.TokenCount(chunkPlusSentence) <= maxChunkSize)
//                     {
//                         // Move next sentence to current chunk
//                         chunk.FullContent.Append(chunk.NextSentence);
//                     }
//                     else
//                     {
//                         // Complete the current chunk and start a new one
//                         chunks.Add(chunk.FullContent.ToString());
//                         chunk.FullContent.Clear().Append(chunk.NextSentence);
//                     }
//
//                     chunk.NextSentence.Clear();
//                     continue;
//                 }
//
//                 // - the current chunk is NOT empty
//                 // - the next sentence is complete and is TOO LONG
//                 case 4:
//                 {
//                     chunks.Add(chunk.FullContent.ToString());
//                     chunk.FullContent.Clear();
//
//                     var moreChunks = this.RecursiveSplit(nextSentence, maxChunkSize, NextSeparatorType(separatorType));
//                     chunks.AddRange(moreChunks.Take(moreChunks.Count - 1));
//                     chunk.NextSentence.Clear().Append(moreChunks.Last());
//                     continue;
//                 }
//             }
//         }
//
//         // If there's something left in the buffers
//         var fullSentenceLeft = chunk.FullContent.ToString();
//         var nextSentenceLeft = chunk.NextSentence.ToString();
//
//         if (fullSentenceLeft.Length > 0 || nextSentenceLeft.Length > 0)
//         {
//             if (this.TokenCount($"{fullSentenceLeft}{nextSentenceLeft}") <= maxChunkSize)
//             {
//                 chunks.Add($"{fullSentenceLeft}{nextSentenceLeft}");
//             }
//             else
//             {
//                 if (fullSentenceLeft.Length > 0) { chunks.Add(fullSentenceLeft); }
//
//                 if (nextSentenceLeft.Length > 0)
//                 {
//                     if (this.TokenCount(nextSentenceLeft) < maxChunkSize)
//                     {
//                         chunks.Add($"{nextSentenceLeft}");
//                     }
//                     else
//                     {
//                         var moreChunks = this.RecursiveSplit(nextSentenceLeft, maxChunkSize, NextSeparatorType(separatorType));
//                         chunks.AddRange(moreChunks);
//                     }
//                 }
//             }
//         }
//
//         return chunks;
//     }
//
//     /// <summary>
//     /// Split text using different separator types.
//     /// - A fragment ends as soon as a separator is found.
//     /// - A fragment can start with a separator.
//     /// - A fragment does not contain two consecutive separators.
//     /// TODO: considering that only one separator type is used and then the list of fragments is discarded, simplify the method
//     /// </summary>
//     internal static List<Fragment> SplitToFragments(string text)
//     {
//         var fragments = new List<Fragment>();
//         var buffer = new StringBuilder();
//
//         void AddFragment(SeparatorTypes type, string separator, ref int cursor, int jump)
//         {
//             fragments.Add(new Fragment
//             {
//                 SeparatorType = type,
//                 Content = $"{buffer}{separator}",
//             });
//             buffer.Clear();
//             cursor += jump;
//         }
//
//         void AddExplicitSeparator(string separator, ref int cursor, int jump)
//         {
//             AddFragment(SeparatorTypes.ExplicitSeparator, separator, ref cursor, jump);
//         }
//
//         void AddPotentialSeparator(string separator, ref int cursor, int jump)
//         {
//             AddFragment(SeparatorTypes.PotentialSeparator, separator, ref cursor, jump);
//         }
//
//         void AddWeakSeparator(string separator, ref int cursor, int jump)
//         {
//             AddFragment(SeparatorTypes.WeakSeparator, separator, ref cursor, jump);
//         }
//
//         for (int i = 0; i < text.Length; i++)
//         {
//             // Note: split options are 4 chars max
//             char char1 = text[i];
//             char? char2 = i + 1 < text.Length ? text[i + 1] : null;
//             char? char3 = i + 2 < text.Length ? text[i + 2] : null;
//             char? char4 = i + 3 < text.Length ? text[i + 3] : null;
//
//             // Check if there's a 4-chars separator
//             string fourCharWord = $"{char1}{char2}{char3}{char4}";
//             if (char4.HasValue)
//             {
//                 if (s_explicitSplitSequences.Contains(fourCharWord))
//                 {
//                     AddExplicitSeparator(fourCharWord, ref i, 3);
//                     continue;
//                 }
//
//                 if (s_potentialSplitSequences.Contains(fourCharWord))
//                 {
//                     AddPotentialSeparator(fourCharWord, ref i, 3);
//                     continue;
//                 }
//
//                 if (s_weakSplitSequences.Contains(fourCharWord))
//                 {
//                     AddWeakSeparator(fourCharWord, ref i, 3);
//                     continue;
//                 }
//             }
//
//             // Check if there's a 3-chars separator
//             string threeCharWord = $"{char1}{char2}{char3}";
//             if (char3.HasValue)
//             {
//                 if (s_explicitSplitSequences.Contains(threeCharWord))
//                 {
//                     AddExplicitSeparator(threeCharWord, ref i, 2);
//                     continue;
//                 }
//
//                 if (s_potentialSplitSequences.Contains(threeCharWord))
//                 {
//                     AddPotentialSeparator(threeCharWord, ref i, 2);
//                     continue;
//                 }
//
//                 if (s_weakSplitSequences.Contains(threeCharWord))
//                 {
//                     AddWeakSeparator(threeCharWord, ref i, 2);
//                     continue;
//                 }
//             }
//
//             // Check if there's a 2-chars separator
//             string twoCharWord = $"{char1}{char2}";
//             if (char2.HasValue)
//             {
//                 if (s_explicitSplitSequences.Contains(twoCharWord))
//                 {
//                     AddExplicitSeparator(twoCharWord, ref i, 1);
//                     continue;
//                 }
//
//                 if (s_potentialSplitSequences.Contains(twoCharWord))
//                 {
//                     AddPotentialSeparator(twoCharWord, ref i, 1);
//                     continue;
//                 }
//
//                 if (s_weakSplitSequences.Contains(twoCharWord))
//                 {
//                     AddWeakSeparator(twoCharWord, ref i, 1);
//                     continue;
//                 }
//             }
//
//             // Check if there's a 1-char separator
//             string oneCharWord = $"{char1}";
//             if (s_explicitSplitSequences.Contains(oneCharWord))
//             {
//                 AddExplicitSeparator(oneCharWord, ref i, 0);
//                 continue;
//             }
//
//             if (s_potentialSplitSequences.Contains(oneCharWord))
//             {
//                 AddPotentialSeparator(oneCharWord, ref i, 0);
//                 continue;
//             }
//
//             if (s_weakSplitSequences.Contains(oneCharWord))
//             {
//                 AddWeakSeparator(oneCharWord, ref i, 0);
//                 continue;
//             }
//
//             buffer.Append(char1);
//         }
//
//         // Content after the last separator
//         if (buffer.Length > 0)
//         {
//             var _ = 0;
//             AddFragment(SeparatorTypes.NotASeparator, separator: "", cursor: ref _, jump: 0);
//         }
//
// #if DEBUGFRAGMENTS
//         this.DebugFragments(fragments);
// #endif
//
//         return fragments;
//     }
//
//     private int TokenCount(string? input)
//     {
//         if (input == null) { return 0; }
//
//         return this._tokenizer.CountTokens(input);
//     }
//
//     #region internals
//
// #if DEBUGCHUNKS
//     private void DebugChunks(List<string> result)
//     {
//         Console.WriteLine("----------------------------------");
//         for (int index = 0; index < result.Count; index++)
//         {
//             Console.WriteLine($"- {index}: \"{result[index]}\" [{this.TokenCount(result[index])} tokens]");
//         }
//
//         Console.WriteLine("----------------------------------");
//     }
// #endif
//
// #if DEBUGFRAGMENTS
//     private void DebugFragments(List<Fragment> fragments)
//     {
//         if (fragments.Count == 0)
//         {
//             Console.WriteLine("No fragments in the list");
//         }
//
//         for (int index = 0; index < fragments.Count; index++)
//         {
//             Fragment fragment = fragments[index];
//             Console.WriteLine($"- {index}: \"{fragment.Content}\"");
//         }
//     }
// #endif
//
//     #endregion
// }

#pragma warning disable CA0000 // reason

#pragma warning restore CA0000
