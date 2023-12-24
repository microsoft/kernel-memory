// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Xunit.Abstractions;

namespace FunctionalTests3P.TestHelpers;

internal sealed class RedirectConsole : TextWriter
{
    private readonly ITestOutputHelper _output;

    public override IFormatProvider FormatProvider => CultureInfo.CurrentCulture;

    public override Encoding Encoding { get; } = Encoding.Default;

    public RedirectConsole(ITestOutputHelper output)
    {
        this._output = output;
    }

    public override void Write(string? value)
    {
        this.Text(value);
    }

    public override void WriteLine(string? value)
    {
        this.Line(value);
    }

    public override void Write(char value)
    {
        this.Text($"{value}");
    }

    public override void WriteLine(char value)
    {
        this.Line($"{value}");
    }

    public override void Write(char[]? buffer)
    {
        if (buffer == null || buffer.Length == 0) { return; }

        var s = new StringBuilder();
        foreach (var c in buffer) { s.Append(c); }

        this.Text(s.ToString());
    }

    public override void WriteLine(char[]? buffer)
    {
        if (buffer == null)
        {
            this.Line();
            return;
        }

        var s = new StringBuilder();
        foreach (var c in buffer) { s.Append(c); }

        this.Line(s.ToString());
    }

    public override void Write(char[] buffer, int index, int count)
    {
        if (buffer.Length == 0 || count <= 0 || index < 0 || buffer.Length - index < count)
        {
            return;
        }

        var s = new StringBuilder();
        for (int i = 0; i < count; i++)
        {
            s.Append(buffer[index + i]);
        }

        this.Text(s.ToString());
    }

    public override void WriteLine(char[] buffer, int index, int count)
    {
        if (count <= 0 || index < 0 || buffer.Length - index < count)
        {
            this.Line();
            return;
        }

        var s = new StringBuilder();
        for (int i = 0; i < count; i++)
        {
            s.Append(buffer[index + i]);
        }

        this.Line(s.ToString());
    }

    public override void Write(ReadOnlySpan<char> buffer)
    {
        if (buffer == null || buffer.Length == 0) { return; }

        var s = new StringBuilder();
        foreach (var c in buffer) { s.Append(c); }

        this.Text(s.ToString());
    }

    public override void WriteLine(ReadOnlySpan<char> buffer)
    {
        if (buffer == null)
        {
            this.Line();
            return;
        }

        var s = new StringBuilder();
        foreach (var c in buffer) { s.Append(c); }

        this.Line(s.ToString());
    }

    public override void Write(StringBuilder? buffer)
    {
        if (buffer == null || buffer.Length == 0) { return; }

        this.Text(buffer.ToString());
    }

    public override void WriteLine(StringBuilder? buffer)
    {
        if (buffer == null)
        {
            this.Line();
            return;
        }

        this.Line(buffer.ToString());
    }

    public override void Write(bool value)
    {
        this.Text(value ? "True" : "False");
    }

    public override void WriteLine(bool value)
    {
        this.Line(value ? "True" : "False");
    }

    public override void Write(int value)
    {
        this.Text(value.ToString(this.FormatProvider));
    }

    public override void WriteLine(int value)
    {
        this.Line(value.ToString(this.FormatProvider));
    }

    public override void Write(uint value)
    {
        this.Text(value.ToString(this.FormatProvider));
    }

    public override void WriteLine(uint value)
    {
        this.Line(value.ToString(this.FormatProvider));
    }

    public override void Write(long value)
    {
        this.Text(value.ToString(this.FormatProvider));
    }

    public override void WriteLine(long value)
    {
        this.Line(value.ToString(this.FormatProvider));
    }

    public override void Write(ulong value)
    {
        this.Text(value.ToString(this.FormatProvider));
    }

    public override void WriteLine(ulong value)
    {
        this.Line(value.ToString(this.FormatProvider));
    }

    public override void Write(float value)
    {
        this.Text(value.ToString(this.FormatProvider));
    }

    public override void WriteLine(float value)
    {
        this.Line(value.ToString(this.FormatProvider));
    }

    public override void Write(double value)
    {
        this.Text(value.ToString(this.FormatProvider));
    }

    public override void WriteLine(double value)
    {
        this.Line(value.ToString(this.FormatProvider));
    }

    public override void Write(decimal value)
    {
        this.Text(value.ToString(this.FormatProvider));
    }

    public override void WriteLine(decimal value)
    {
        this.Line(value.ToString(this.FormatProvider));
    }

    public override void Write(object? value)
    {
        if (value != null)
        {
            if (value is IFormattable f)
            {
                this.Text(f.ToString(null, this.FormatProvider));
            }
            else
            {
                this.Text(value.ToString());
            }
        }
    }

    public override void WriteLine(object? value)
    {
        if (value != null)
        {
            if (value is IFormattable f)
            {
                this.Line(f.ToString(null, this.FormatProvider));
            }
            else
            {
                this.Line(value.ToString());
            }
        }
        else
        {
            this.Line();
        }
    }

    public override void Write([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg0)
    {
        this.Text(string.Format(this.FormatProvider, format, arg0));
    }

    public override void WriteLine([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg0)
    {
        this.Line(string.Format(this.FormatProvider, format, arg0));
    }

    public override void Write([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg0, object? arg1)
    {
        this.Text(string.Format(this.FormatProvider, format, arg0, arg1));
    }

    public override void WriteLine([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg0, object? arg1)
    {
        this.Line(string.Format(this.FormatProvider, format, arg0, arg1));
    }

    public override void Write([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg0, object? arg1, object? arg2)
    {
        this.Text(string.Format(this.FormatProvider, format, arg0, arg1, arg2));
    }

    public override void WriteLine([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg0, object? arg1, object? arg2)
    {
        this.Line(string.Format(this.FormatProvider, format, arg0, arg1, arg2));
    }

    public override void Write([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, params object?[] arg)
    {
        this.Text(string.Format(this.FormatProvider, format, arg));
    }

    public override void WriteLine([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, params object?[] arg)
    {
        this.Line(string.Format(this.FormatProvider, format, arg));
    }

    private void Text(string? s)
    {
        if (string.IsNullOrEmpty(s)) { return; }

        try
        {
            this._output.WriteLine(s);
        }
        catch (InvalidOperationException e) when (e.Message.Contains("no currently active test", StringComparison.OrdinalIgnoreCase))
        {
            // NOOP: Xunit thread out of scope
        }
    }

    private void Line(string? s = null)
    {
        try
        {
            this._output.WriteLine(s);
        }
        catch (InvalidOperationException e) when (e.Message.Contains("no currently active test", StringComparison.OrdinalIgnoreCase))
        {
            // NOOP: Xunit thread out of scope
        }
    }
}
