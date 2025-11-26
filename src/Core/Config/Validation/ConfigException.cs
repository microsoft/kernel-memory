namespace KernelMemory.Core.Config.Validation;

/// <summary>
/// Exception thrown when configuration validation fails
/// </summary>
public class ConfigException : Exception
{
    /// <summary>
    /// JSON path where the error occurred
    /// </summary>
    public string ConfigPath { get; }

    /// <summary>
    /// Creates a new configuration exception
    /// </summary>
    /// <param name="configPath">JSON path (e.g., "Nodes.personal.ContentIndex.Path")</param>
    /// <param name="message">Human-readable error message</param>
    public ConfigException(string configPath, string message)
        : base($"Configuration error at '{configPath}': {message}")
    {
        this.ConfigPath = configPath;
    }

    /// <summary>
    /// Creates a new configuration exception with inner exception
    /// </summary>
    /// <param name="configPath">JSON path</param>
    /// <param name="message">Human-readable error message</param>
    /// <param name="innerException">Underlying exception</param>
    public ConfigException(string configPath, string message, Exception innerException)
        : base($"Configuration error at '{configPath}': {message}", innerException)
    {
        this.ConfigPath = configPath;
    }

    public ConfigException() : base()
    {
        this.ConfigPath = string.Empty;
    }

    public ConfigException(string? message) : base(message)
    {
        this.ConfigPath = string.Empty;
    }

    public ConfigException(string? message, Exception? innerException) : base(message, innerException)
    {
        this.ConfigPath = string.Empty;
    }
}
