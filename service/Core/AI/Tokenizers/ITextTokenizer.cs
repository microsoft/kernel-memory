// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Text;

namespace Microsoft.KernelMemory.AI.Tokenizers;

public interface ITextTokenizer
{
    public int CountTokens(string text);
    public int CountTokens(StringBuilder? stringBuilder);
    public int CountTokens(char[]? chars);
    public int CountTokens(IEnumerable<char>? chars);

    public List<int> Encode(string text);
    public List<int> Encode(StringBuilder? stringBuilder);
    public List<int> Encode(char[]? chars);
    public List<int> Encode(IEnumerable<char>? chars);
}
