using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace KernelMemory.Main.CLI.OutputFormatters;

/// <summary>
/// Formats output as YAML for human and machine-readable consumption.
/// </summary>
public class YamlOutputFormatter : IOutputFormatter
{
    private readonly ISerializer _yamlSerializer;

    public string Verbosity { get; }

    public YamlOutputFormatter(string verbosity)
    {
        this.Verbosity = verbosity;
        this._yamlSerializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
    }

    public void Format(object data)
    {
        if (this.Verbosity.Equals("silent", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var yaml = this._yamlSerializer.Serialize(data);
        Console.WriteLine(yaml);
    }

    public void FormatError(string errorMessage)
    {
        var error = new { error = errorMessage };
        var yaml = this._yamlSerializer.Serialize(error);
        Console.Error.WriteLine(yaml);
    }

    public void FormatList<T>(IEnumerable<T> items, long totalCount, int skip, int take)
    {
        if (this.Verbosity.Equals("silent", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var result = new
        {
            items = items,
            pagination = new
            {
                totalCount = totalCount,
                skip = skip,
                take = take,
                returned = items.Count()
            }
        };

        var yaml = this._yamlSerializer.Serialize(result);
        Console.WriteLine(yaml);
    }
}
