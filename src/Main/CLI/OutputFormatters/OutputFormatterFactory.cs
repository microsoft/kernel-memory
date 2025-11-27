namespace KernelMemory.Main.CLI.OutputFormatters;

/// <summary>
/// Factory for creating output formatters based on settings.
/// </summary>
public static class OutputFormatterFactory
{
    /// <summary>
    /// Creates an output formatter based on the provided settings.
    /// </summary>
    /// <param name="settings">Global options containing format and verbosity settings.</param>
    /// <returns>An appropriate IOutputFormatter instance.</returns>
    public static IOutputFormatter Create(GlobalOptions settings)
    {
        var format = settings.Format.ToLowerInvariant();
        var verbosity = settings.Verbosity;
        var useColors = !settings.NoColor;

        return format switch
        {
            "json" => new JsonOutputFormatter(verbosity),
            "yaml" => new YamlOutputFormatter(verbosity),
            "human" => new HumanOutputFormatter(verbosity, useColors),
            _ => new HumanOutputFormatter(verbosity, useColors)
        };
    }
}
