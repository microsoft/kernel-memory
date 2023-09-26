// Copyright (c) Microsoft. All rights reserved.

using FunctionalTests.TestHelpers;
using Xunit.Abstractions;

namespace FunctionalTests.Service;

public abstract class BaseTestCase : IDisposable
{
    private readonly RedirectConsole _output;

    public BaseTestCase(ITestOutputHelper output)
    {
        this._output = new RedirectConsole(output);
        Console.SetOut(this._output);
    }

    public void Dispose()
    {
        this._output.Dispose();
    }

    protected void Log(string text)
    {
        this._output.WriteLine(text);
    }
}
