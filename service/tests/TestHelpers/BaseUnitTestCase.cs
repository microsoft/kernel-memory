// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.KM.TestHelpers;

public abstract class BaseUnitTestCase : IDisposable
{
    private readonly RedirectConsole _output;

    protected BaseUnitTestCase(ITestOutputHelper output)
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
