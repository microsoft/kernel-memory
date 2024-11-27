// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.KernelMemory.AI;

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
