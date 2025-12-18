// Copyright (c) Microsoft. All rights reserved.

using Serilog.Events;

namespace KernelMemory.Core;

/// <summary>
/// Centralized constants for the Core module.
/// Organized in nested classes by domain for maintainability and discoverability.
/// All magic values should be defined here rather than hardcoded throughout the codebase.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Constants for search functionality including FTS and vector search.
    /// </summary>
    public static class SearchDefaults
    {
        /// <summary>
        /// Default minimum relevance score threshold (0.0-1.0).
        /// Results below this score are filtered out.
        /// </summary>
        public const float DefaultMinRelevance = 0.3f;

        /// <summary>
        /// Default maximum number of results to return per search.
        /// </summary>
        public const int DefaultLimit = 20;

        /// <summary>
        /// Default search timeout in seconds per node.
        /// </summary>
        public const int DefaultSearchTimeoutSeconds = 30;

        /// <summary>
        /// Default maximum results to retrieve from each node (memory safety).
        /// Prevents memory exhaustion from large result sets.
        /// </summary>
        public const int DefaultMaxResultsPerNode = 1000;

        /// <summary>
        /// Default node weight for relevance scoring.
        /// </summary>
        public const float DefaultNodeWeight = 1.0f;

        /// <summary>
        /// Default search index weight for relevance scoring.
        /// </summary>
        public const float DefaultIndexWeight = 1.0f;

        /// <summary>
        /// BM25 score normalization divisor for exponential mapping.
        /// Maps BM25 range [-10, 0] to [0.37, 1.0] using exp(score/divisor).
        /// </summary>
        public const double Bm25NormalizationDivisor = 10.0;

        /// <summary>
        /// Maximum nesting depth for query parentheses.
        /// Prevents DoS attacks via deeply nested queries.
        /// </summary>
        public const int MaxQueryDepth = 10;

        /// <summary>
        /// Maximum number of boolean operators (AND/OR/NOT) in a single query.
        /// Prevents query complexity attacks.
        /// </summary>
        public const int MaxBooleanOperators = 50;

        /// <summary>
        /// Maximum length of a field value in query (characters).
        /// Prevents oversized query values.
        /// </summary>
        public const int MaxFieldValueLength = 1000;

        /// <summary>
        /// Maximum time allowed for query parsing (milliseconds).
        /// Prevents regex catastrophic backtracking.
        /// </summary>
        public const int QueryParseTimeoutMs = 1000;

        /// <summary>
        /// Default snippet length in characters.
        /// </summary>
        public const int DefaultSnippetLength = 200;

        /// <summary>
        /// Default maximum number of snippets per result.
        /// </summary>
        public const int DefaultMaxSnippetsPerResult = 1;

        /// <summary>
        /// Default snippet separator between multiple snippets.
        /// </summary>
        public const string DefaultSnippetSeparator = "...";

        /// <summary>
        /// Default highlight prefix marker.
        /// </summary>
        public const string DefaultHighlightPrefix = "<mark>";

        /// <summary>
        /// Default highlight suffix marker.
        /// </summary>
        public const string DefaultHighlightSuffix = "</mark>";

        /// <summary>
        /// Diminishing returns multipliers for aggregating multiple appearances of same record.
        /// First appearance: 1.0 (full weight)
        /// Second appearance: 0.5 (50% boost)
        /// Third appearance: 0.25 (25% boost)
        /// Fourth appearance: 0.125 (12.5% boost)
        /// Each subsequent multiplier is half of the previous.
        /// </summary>
        public static readonly float[] DefaultDiminishingMultipliers = [1.0f, 0.5f, 0.25f, 0.125f];

        /// <summary>
        /// Wildcard character for "all nodes" in node selection.
        /// </summary>
        public const string AllNodesWildcard = "*";

        /// <summary>
        /// Maximum relevance score (scores are capped at this value).
        /// </summary>
        public const float MaxRelevanceScore = 1.0f;

        /// <summary>
        /// Minimum relevance score.
        /// </summary>
        public const float MinRelevanceScore = 0.0f;

        /// <summary>
        /// Default FTS index ID used when not specified in configuration.
        /// This is the identifier assigned to search results from the full-text search index.
        /// </summary>
        public const string DefaultFtsIndexId = "fts-main";
    }

    /// <summary>
    /// Constants for embedding generation including known model dimensions,
    /// default configurations, and batch sizes.
    /// </summary>
    public static class EmbeddingDefaults
    {
        /// <summary>
        /// Default batch size for embedding generation requests.
        /// Configurable per provider, but this is the default.
        /// </summary>
        public const int DefaultBatchSize = 10;

        /// <summary>
        /// Default Ollama model for embeddings.
        /// </summary>
        public const string DefaultOllamaModel = "qwen3-embedding:0.6b";

        /// <summary>
        /// Default Ollama base URL.
        /// </summary>
        public const string DefaultOllamaBaseUrl = "http://localhost:11434";

        /// <summary>
        /// Default HuggingFace model for embeddings.
        /// </summary>
        public const string DefaultHuggingFaceModel = "sentence-transformers/all-MiniLM-L6-v2";

        /// <summary>
        /// Default HuggingFace Inference API base URL.
        /// </summary>
        public const string DefaultHuggingFaceBaseUrl = "https://api-inference.huggingface.co";

        /// <summary>
        /// Default OpenAI API base URL.
        /// </summary>
        public const string DefaultOpenAIBaseUrl = "https://api.openai.com";

        /// <summary>
        /// Azure OpenAI API version.
        /// </summary>
        public const string AzureOpenAIApiVersion = "2024-02-01";

        /// <summary>
        /// Known model dimensions for common embedding models.
        /// These values are fixed per model and used for validation and cache key generation.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, int> KnownModelDimensions = new Dictionary<string, int>
        {
            // Ollama models
            ["qwen3-embedding"] = 1024,
            ["nomic-embed-text"] = 768,
            ["embeddinggemma"] = 768,

            // OpenAI models
            ["text-embedding-ada-002"] = 1536,
            ["text-embedding-3-small"] = 1536,
            ["text-embedding-3-large"] = 3072,

            // HuggingFace models
            ["sentence-transformers/all-MiniLM-L6-v2"] = 384,
            ["BAAI/bge-base-en-v1.5"] = 768
        };

        /// <summary>
        /// Try to get the dimensions for a known model.
        /// </summary>
        /// <param name="modelName">The model name to look up.</param>
        /// <param name="dimensions">The dimensions if found, 0 otherwise.</param>
        /// <returns>True if the model is known, false otherwise.</returns>
        public static bool TryGetDimensions(string modelName, out int dimensions)
        {
            return KnownModelDimensions.TryGetValue(modelName, out dimensions);
        }
    }

    /// <summary>
    /// Constants for HTTP retry/backoff used by external providers (embeddings, etc.).
    /// </summary>
    public static class HttpRetryDefaults
    {
        /// <summary>
        /// Maximum attempts including the first try.
        /// </summary>
        public const int MaxAttempts = 5;

        /// <summary>
        /// Base delay for exponential backoff.
        /// </summary>
        public const int BaseDelayMs = 200;

        /// <summary>
        /// Maximum delay between attempts.
        /// </summary>
        public const int MaxDelayMs = 5000;
    }

    /// <summary>
    /// Constants for the logging system including file rotation, log levels,
    /// and output formatting.
    /// </summary>
    public static class LoggingDefaults
    {
        /// <summary>
        /// Default maximum file size before rotation (100MB).
        /// Balances history retention with disk usage.
        /// </summary>
        public const long DefaultFileSizeLimitBytes = 100 * 1024 * 1024;

        /// <summary>
        /// Default number of log files to retain (30 files).
        /// Approximately 1 month of daily logs or ~3GB max storage.
        /// </summary>
        public const int DefaultRetainedFileCountLimit = 30;

        /// <summary>
        /// Default minimum log level for file output.
        /// Information level provides useful diagnostics without excessive verbosity.
        /// </summary>
        public const LogEventLevel DefaultFileLogLevel = LogEventLevel.Information;

        /// <summary>
        /// Default minimum log level for console/stderr output.
        /// Only warnings and errors appear on stderr by default.
        /// </summary>
        public const LogEventLevel DefaultConsoleLogLevel = LogEventLevel.Warning;

        /// <summary>
        /// Environment variable for .NET runtime environment detection.
        /// Takes precedence over ASPNETCORE_ENVIRONMENT.
        /// </summary>
        public const string DotNetEnvironmentVariable = "DOTNET_ENVIRONMENT";

        /// <summary>
        /// Fallback environment variable for ASP.NET Core applications.
        /// Used when DOTNET_ENVIRONMENT is not set.
        /// </summary>
        public const string AspNetCoreEnvironmentVariable = "ASPNETCORE_ENVIRONMENT";

        /// <summary>
        /// Default environment when no environment variable is set.
        /// Defaults to Development for developer safety (full logging enabled).
        /// </summary>
        public const string DefaultEnvironment = "Development";

        /// <summary>
        /// Production environment name for comparison.
        /// Sensitive data is scrubbed only in Production.
        /// </summary>
        public const string ProductionEnvironment = "Production";

        /// <summary>
        /// Placeholder text for redacted sensitive data.
        /// Used to indicate data was intentionally removed from logs.
        /// </summary>
        public const string RedactedPlaceholder = "[REDACTED]";

        /// <summary>
        /// Human-readable output template for log messages.
        /// Includes timestamp, level, source context, message, and optional exception.
        /// </summary>
        public const string HumanReadableOutputTemplate =
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";

        /// <summary>
        /// Compact output template for console (stderr) output.
        /// Shorter format suitable for CLI error reporting.
        /// </summary>
        public const string ConsoleOutputTemplate =
            "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

        /// <summary>
        /// Empty trace ID value (32 zeros) used when no Activity is present.
        /// Indicates no distributed tracing context is available.
        /// </summary>
        public const string EmptyTraceId = "00000000000000000000000000000000";

        /// <summary>
        /// Empty span ID value (16 zeros) used when no Activity is present.
        /// Indicates no distributed tracing context is available.
        /// </summary>
        public const string EmptySpanId = "0000000000000000";
    }

    /// <summary>
    /// Constants for application configuration and setup.
    /// </summary>
    public static class ConfigDefaults
    {
        /// <summary>
        /// Default configuration file name.
        /// </summary>
        public const string DefaultConfigFileName = "config.json";

        /// <summary>
        /// Default configuration directory name in user's home directory.
        /// </summary>
        public const string DefaultConfigDirName = ".km";
    }

    /// <summary>
    /// Constants for application exit codes and CLI behavior.
    /// </summary>
    public static class App
    {
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

    /// <summary>
    /// Constants for database and storage operations.
    /// </summary>
    public static class Database
    {
        /// <summary>
        /// SQLite busy timeout in milliseconds for handling concurrent access.
        /// Waits up to this duration before throwing a busy exception.
        /// </summary>
        public const int SqliteBusyTimeoutMs = 5000;

        /// <summary>
        /// Maximum length for MIME type field in content storage.
        /// Prevents excessively long MIME type values.
        /// </summary>
        public const int MaxMimeTypeLength = 255;

        /// <summary>
        /// Default snippet preview length in characters for SQL queries.
        /// Used when displaying content excerpts in search results.
        /// </summary>
        public const int DefaultSqlSnippetLength = 200;
    }
}
