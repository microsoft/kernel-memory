namespace KernelMemory.Main;

/// <summary>
/// Application-wide constants.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Default configuration file name.
    /// </summary>
    public const string DefaultConfigFileName = "config.json";

    /// <summary>
    /// Default configuration directory name in user's home directory.
    /// </summary>
    public const string DefaultConfigDirName = ".km";

    /// <summary>
    /// Exit code for successful operation.
    /// </summary>
    public const int ExitCodeSuccess = 0;

    /// <summary>
    /// Exit code for user errors (bad input, not found, validation failure).
    /// </summary>
    public const int ExitCodeUserError = 1;

    /// <summary>
    /// Exit code for system errors (storage failure, config error, unexpected exception).
    /// </summary>
    public const int ExitCodeSystemError = 2;

    /// <summary>
    /// Default pagination size for list operations.
    /// </summary>
    public const int DefaultPageSize = 20;

    /// <summary>
    /// Maximum content length to display in truncated view (characters).
    /// </summary>
    public const int MaxContentDisplayLength = 100;
}
