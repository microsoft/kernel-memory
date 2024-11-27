// Copyright (c) Microsoft. All rights reserved.

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace
namespace Microsoft.KernelMemory.AI.OpenAI;

/// <summary>
/// GPT3 tokenizer
/// </summary>
public sealed class GPT3Tokenizer : P50KTokenizer
{
}

/// <summary>
/// gpt-3.5-turbo
/// gpt-3.5-turbo-*
/// gpt-4
/// text-embedding-ada-002
/// text-embedding-3-small
/// text-embedding-3-large
/// </summary>
public sealed class GPT4Tokenizer : CL100KTokenizer
{
}

/// <summary>
/// GPT 4o / 4o mini tokenizer
/// gpt-4o
/// gpt-4o-*
/// </summary>
// ReSharper disable once InconsistentNaming
public sealed class GPT4oTokenizer : O200KTokenizer
{
}
