// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.KernelMemory.AI;

public static class TokenizerFactory
{
    public static ITextTokenizer? GetTokenizerForEncoding(string encodingId)
    {
        encodingId = encodingId.ToLowerInvariant();

        switch (encodingId.ToLowerInvariant())
        {
            case "p50k":
                return new P50KTokenizer();

            case "cl100k":
                return new CL100KTokenizer();

            case "o200k":
                return new O200KTokenizer();
        }

        return null;
    }

    public static ITextTokenizer? GetTokenizerForModel(string modelId)
    {
        try
        {
            return new TiktokenTokenizer(modelId);
        }
        catch (KernelMemoryException)
        {
            // ignore
        }

        modelId = modelId.ToLowerInvariant();

        if (modelId.StartsWith("text-embedding-", StringComparison.Ordinal)
            || modelId.StartsWith("gpt-3.5-", StringComparison.Ordinal)
            || modelId.StartsWith("gpt-4-", StringComparison.Ordinal))
        {
            return new CL100KTokenizer();
        }

        if (modelId.StartsWith("gpt-4o-", StringComparison.Ordinal))
        {
            return new O200KTokenizer();
        }

        switch (modelId.ToLowerInvariant())
        {
            case "code-davinci-001":
            case "code-davinci-002":
            case "text-davinci-002":
            case "text-davinci-003":
                return new P50KTokenizer();

            case "gpt-3.5-turbo":
            case "gpt-4":
                return new CL100KTokenizer();

            case "gpt-4o":
                return new O200KTokenizer();
        }

        return null;
    }
}
