// Copyright (c) Microsoft. All rights reserved.

using System.Reflection;
using Microsoft.KernelMemory;
using Xunit.Abstractions;

namespace FunctionalTests.TestHelpers;

public abstract class BaseTestCase : IDisposable
{
    protected const string NotFound = "INFO NOT FOUND";

    private readonly IConfiguration _cfg;
    private readonly RedirectConsole _output;

    protected IConfiguration Configuration => this._cfg;

    protected BaseTestCase(IConfiguration cfg, ITestOutputHelper output)
    {
        this._cfg = cfg;
        this._output = new RedirectConsole(output);
        Console.SetOut(this._output);
    }

    protected IKernelMemory GetMemoryWebClient()
    {
        string endpoint = this.Configuration.GetSection("ServiceAuthorization").GetValue<string>("Endpoint", "http://127.0.0.1:9001/")!;
        string? apiKey = this.Configuration.GetSection("ServiceAuthorization").GetValue<string>("AccessKey");
        return new MemoryWebClient(endpoint, apiKey: apiKey);
    }

    // Find the "Fixtures" directory (inside the project, requires source code)
    protected string? FindFixturesDir()
    {
        // start from the location of the executing assembly, and traverse up max 5 levels
        var path = Path.GetDirectoryName(Path.GetFullPath(Assembly.GetExecutingAssembly().Location));
        for (var i = 0; i < 5; i++)
        {
            Console.WriteLine($"Checking '{path}'");
            var test = Path.Join(path, "Fixtures");
            if (Directory.Exists(test)) { return test; }

            // up one level
            path = Path.GetDirectoryName(path);
        }

        return null;
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            this._output.Dispose();
        }
    }

    protected void Log(string text)
    {
        this._output.WriteLine(text);
    }
}
