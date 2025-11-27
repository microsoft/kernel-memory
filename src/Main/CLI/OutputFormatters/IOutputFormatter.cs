namespace KernelMemory.Main.CLI.OutputFormatters;

/// <summary>
/// Interface for formatting command output to stdout/stderr.
/// Different formatters support different output formats (human-readable, JSON, YAML).
/// </summary>
public interface IOutputFormatter
{
    /// <summary>
    /// Gets the verbosity level for this formatter.
    /// </summary>
    string Verbosity { get; }

    /// <summary>
    /// Formats and outputs a success message or data object to stdout.
    /// </summary>
    /// <param name="data">The data to format and output.</param>
    void Format(object data);

    /// <summary>
    /// Formats and outputs an error message to stderr.
    /// </summary>
    /// <param name="errorMessage">The error message to output.</param>
    void FormatError(string errorMessage);

    /// <summary>
    /// Formats and outputs a list of items with optional pagination info.
    /// </summary>
    /// <param name="items">The list of items to format.</param>
    /// <param name="totalCount">Total count of items (for pagination).</param>
    /// <param name="skip">Number of items skipped.</param>
    /// <param name="take">Number of items taken.</param>
    void FormatList<T>(IEnumerable<T> items, long totalCount, int skip, int take);
}
