namespace KernelMemory.Core.Config.Validation;

/// <summary>
/// Interface for configuration objects that can validate themselves
/// </summary>
public interface IValidatable
{
    /// <summary>
    /// Validates the configuration object
    /// </summary>
    /// <param name="path">JSON path for error reporting (e.g., "Nodes.personal.ContentIndex")</param>
    /// <exception cref="ConfigException">Thrown when validation fails</exception>
    void Validate(string path);
}
