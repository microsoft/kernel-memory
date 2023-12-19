// Copyright (c) Microsoft. All rights reserved.

namespace ManualTests;

public static class Program
{
    public static async Task Main()
    {
        await AzureAISearch.Program.RunAsync();
        await Qdrant.Program.RunAsync();
    }
}
