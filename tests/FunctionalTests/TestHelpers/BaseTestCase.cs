// Copyright (c) Microsoft. All rights reserved.

using Xunit.Abstractions;

namespace FunctionalTests.TestHelpers;

public abstract class BaseTestCase : IDisposable
{
    private readonly RedirectConsole _output;

    protected BaseTestCase(ITestOutputHelper output)
    {
        this._output = new RedirectConsole(output);
        Console.SetOut(this._output);
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected void Log(string text)
    {
        this._output.WriteLine(text);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            this._output.Dispose();
        }
    }
}
