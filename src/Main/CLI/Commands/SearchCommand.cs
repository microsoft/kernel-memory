// Copyright (c) Microsoft. All rights reserved.
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using KernelMemory.Core.Search;
using KernelMemory.Core.Search.Models;
using KernelMemory.Main.CLI.Exceptions;
using Spectre.Console;
using Spectre.Console.Cli;

namespace KernelMemory.Main.CLI.Commands;

/// <summary>
/// Settings for the search command with all 13 flags from requirements.
/// </summary>
public class SearchCommandSettings : GlobalOptions
{
    [CommandArgument(0, "<query>")]
    [Description("Search query (infix syntax or MongoDB JSON)")]
    public required string Query { get; init; }

    // Node Selection (Q8)
    [CommandOption("--nodes")]
    [Description("Specific nodes to search (comma-separated, overrides config)")]
    public string? Nodes { get; init; }

    [CommandOption("--exclude-nodes")]
    [Description("Nodes to exclude from search (comma-separated)")]
    public string? ExcludeNodes { get; init; }

    // Index Selection
    [CommandOption("--indexes")]
    [Description("Specific indexes to search (supports 'indexId' and 'nodeId:indexId' syntax)")]
    public string? Indexes { get; init; }

    [CommandOption("--exclude-indexes")]
    [Description("Indexes to exclude from search (same syntax as --indexes)")]
    public string? ExcludeIndexes { get; init; }

    // Result Control
    [CommandOption("--limit")]
    [Description("Max results to return (default: 20)")]
    [DefaultValue(20)]
    public int Limit { get; init; } = 20;

    [CommandOption("--offset")]
    [Description("Pagination offset (default: 0)")]
    [DefaultValue(0)]
    public int Offset { get; init; }

    [CommandOption("--min-relevance")]
    [Description("Minimum relevance score threshold (0.0-1.0, default: 0.3)")]
    [DefaultValue(0.3f)]
    public float MinRelevance { get; init; } = 0.3f;

    [CommandOption("--max-results-per-node")]
    [Description("Memory safety limit per node (default: 1000)")]
    public int? MaxResultsPerNode { get; init; }

    // Content Control
    [CommandOption("--snippet")]
    [Description("Return snippets instead of full content")]
    public bool Snippet { get; init; }

    [CommandOption("--snippet-length")]
    [Description("Override snippet length (default: 200 chars)")]
    public int? SnippetLength { get; init; }

    [CommandOption("--highlight")]
    [Description("Wrap matched terms in highlight markers")]
    public bool Highlight { get; init; }

    // Performance
    [CommandOption("--timeout")]
    [Description("Search timeout per node in seconds (default: 30)")]
    public int? Timeout { get; init; }

    [CommandOption("--node-weights")]
    [Description("Override node weights at query time (format: node1:weight,node2:weight)")]
    public string? NodeWeights { get; init; }

    // Validation
    [CommandOption("--validate")]
    [Description("Validate query without executing")]
    public bool ValidateOnly { get; init; }

    public override ValidationResult Validate()
    {
        var baseResult = base.Validate();
        if (!baseResult.Successful)
        {
            return baseResult;
        }

        if (string.IsNullOrWhiteSpace(this.Query))
        {
            return ValidationResult.Error("Query cannot be empty");
        }

        if (this.Limit <= 0)
        {
            return ValidationResult.Error("Limit must be > 0");
        }

        if (this.Offset < 0)
        {
            return ValidationResult.Error("Offset must be >= 0");
        }

        if (this.MinRelevance < 0 || this.MinRelevance > 1.0f)
        {
            return ValidationResult.Error("MinRelevance must be between 0.0 and 1.0");
        }

        if (this.MaxResultsPerNode.HasValue && this.MaxResultsPerNode.Value <= 0)
        {
            return ValidationResult.Error("MaxResultsPerNode must be > 0");
        }

        if (this.SnippetLength.HasValue && this.SnippetLength.Value <= 0)
        {
            return ValidationResult.Error("SnippetLength must be > 0");
        }

        if (this.Timeout.HasValue && this.Timeout.Value <= 0)
        {
            return ValidationResult.Error("Timeout must be > 0");
        }

        return ValidationResult.Success();
    }
}

/// <summary>
/// Command to search across nodes and indexes.
/// Implements all 13 flags from requirements document.
/// </summary>
public class SearchCommand : BaseCommand<SearchCommandSettings>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SearchCommand"/> class.
    /// </summary>
    /// <param name="config">Application configuration (injected by DI).</param>
    public SearchCommand(KernelMemory.Core.Config.AppConfig config) : base(config)
    {
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Top-level command handler must catch all exceptions to return appropriate exit codes and error messages")]
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        SearchCommandSettings settings)
    {
        try
        {
            var formatter = CLI.OutputFormatters.OutputFormatterFactory.Create(settings);

            // Create search service
            var searchService = this.CreateSearchService();

            // If validate flag is set, just validate and return
            if (settings.ValidateOnly)
            {
                return await this.ValidateQueryAsync(searchService, settings, formatter).ConfigureAwait(false);
            }

            // Build search request
            var request = this.BuildSearchRequest(settings);

            // Execute search
            var response = await searchService.SearchAsync(request, CancellationToken.None).ConfigureAwait(false);

            // Format and display results
            this.FormatSearchResults(response, settings, formatter);

            return Constants.ExitCodeSuccess;
        }
        catch (DatabaseNotFoundException)
        {
            // First-run scenario: no database exists yet
            this.ShowFirstRunMessage(settings);
            return Constants.ExitCodeSuccess; // Not a user error
        }
        catch (Core.Search.Exceptions.SearchException ex)
        {
            var formatter = CLI.OutputFormatters.OutputFormatterFactory.Create(settings);
            formatter.FormatError($"Search error: {ex.Message}");
            return Constants.ExitCodeUserError;
        }
        catch (Exception ex)
        {
            var formatter = CLI.OutputFormatters.OutputFormatterFactory.Create(settings);
            return this.HandleError(ex, formatter);
        }
    }

    /// <summary>
    /// Validates a query without executing it.
    /// </summary>
    /// <param name="searchService">The search service to use for validation.</param>
    /// <param name="settings">The command settings.</param>
    /// <param name="formatter">The output formatter.</param>
    /// <returns>Exit code (0 for valid, 1 for invalid).</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance",
        Justification = "Using interface provides flexibility for testing and future implementations")]
    private async Task<int> ValidateQueryAsync(
        ISearchService searchService,
        SearchCommandSettings settings,
        CLI.OutputFormatters.IOutputFormatter formatter)
    {
        var result = await searchService.ValidateQueryAsync(settings.Query, CancellationToken.None).ConfigureAwait(false);

        if (settings.Format.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            formatter.Format(result);
        }
        else if (settings.Format.Equals("yaml", StringComparison.OrdinalIgnoreCase))
        {
            formatter.Format(result);
        }
        else
        {
            // Human format
            if (result.IsValid)
            {
                AnsiConsole.MarkupLine("[green]✓ Query is valid[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[red]✗ Query syntax error[/]");
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[red]{result.ErrorMessage}[/]");
                if (result.ErrorPosition.HasValue)
                {
                    AnsiConsole.MarkupLine($"[dim]Position: {result.ErrorPosition.Value}[/]");
                }
            }

            if (result.AvailableFields.Length > 0)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[dim]Available fields:[/]");
                foreach (var field in result.AvailableFields)
                {
                    AnsiConsole.MarkupLine($"  [cyan]{field}[/]");
                }
            }
        }

        return result.IsValid ? Constants.ExitCodeSuccess : Constants.ExitCodeUserError;
    }

    /// <summary>
    /// Builds a SearchRequest from command settings.
    /// </summary>
    /// <param name="settings">The command settings.</param>
    /// <returns>A configured SearchRequest.</returns>
    private SearchRequest BuildSearchRequest(SearchCommandSettings settings)
    {
        var request = new SearchRequest
        {
            Query = settings.Query,
            Limit = settings.Limit,
            Offset = settings.Offset,
            MinRelevance = settings.MinRelevance,
            SnippetOnly = settings.Snippet,
            Highlight = settings.Highlight
        };

        // Node selection
        if (!string.IsNullOrEmpty(settings.Nodes))
        {
            request.Nodes = settings.Nodes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        if (!string.IsNullOrEmpty(settings.ExcludeNodes))
        {
            request.ExcludeNodes = settings.ExcludeNodes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        // Index selection
        if (!string.IsNullOrEmpty(settings.Indexes))
        {
            request.SearchIndexes = settings.Indexes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        if (!string.IsNullOrEmpty(settings.ExcludeIndexes))
        {
            request.ExcludeIndexes = settings.ExcludeIndexes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        // Optional parameters
        if (settings.MaxResultsPerNode.HasValue)
        {
            request.MaxResultsPerNode = settings.MaxResultsPerNode.Value;
        }

        if (settings.SnippetLength.HasValue)
        {
            request.SnippetLength = settings.SnippetLength.Value;
        }

        if (settings.Timeout.HasValue)
        {
            request.TimeoutSeconds = settings.Timeout.Value;
        }

        // Parse node weights
        if (!string.IsNullOrEmpty(settings.NodeWeights))
        {
            request.NodeWeights = this.ParseNodeWeights(settings.NodeWeights);
        }

        return request;
    }

    /// <summary>
    /// Parses node weights from CLI format: "node1:1.0,node2:0.5"
    /// </summary>
    /// <param name="nodeWeights">The node weights string to parse.</param>
    /// <returns>Dictionary of node ID to weight.</returns>
    private Dictionary<string, float> ParseNodeWeights(string nodeWeights)
    {
        var weights = new Dictionary<string, float>();

        var pairs = nodeWeights.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var pair in pairs)
        {
            var parts = pair.Split(':', StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
            {
                throw new ArgumentException($"Invalid node weight format: '{pair}'. Expected format: 'node:weight'");
            }

            var nodeId = parts[0];
            if (!float.TryParse(parts[1], out var weight))
            {
                throw new ArgumentException($"Invalid weight value for node '{nodeId}': '{parts[1]}'. Must be a number.");
            }

            if (weight < 0 || weight > 1.0f)
            {
                throw new ArgumentException($"Weight for node '{nodeId}' must be between 0.0 and 1.0, got: {weight}");
            }

            weights[nodeId] = weight;
        }

        return weights;
    }

    /// <summary>
    /// Formats and displays search results.
    /// </summary>
    /// <param name="response">The search response to format.</param>
    /// <param name="settings">The command settings.</param>
    /// <param name="formatter">The output formatter.</param>
    private void FormatSearchResults(
        SearchResponse response,
        SearchCommandSettings settings,
        CLI.OutputFormatters.IOutputFormatter formatter)
    {
        if (settings.Format.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            formatter.Format(response);
        }
        else if (settings.Format.Equals("yaml", StringComparison.OrdinalIgnoreCase))
        {
            formatter.Format(response);
        }
        else
        {
            // Human format - create a table
            this.FormatSearchResultsHuman(response, settings);
        }
    }

    /// <summary>
    /// Formats search results in human-readable format (table).
    /// </summary>
    /// <param name="response">The search response to format.</param>
    /// <param name="settings">The command settings.</param>
    private void FormatSearchResultsHuman(SearchResponse response, SearchCommandSettings settings)
    {
        if (response.Results.Length == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No results found[/]");
            return;
        }

        // Create table
        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn("ID");
        table.AddColumn("Node");
        table.AddColumn("Relevance");
        table.AddColumn("Title/Content");

        foreach (var result in response.Results)
        {
            var id = result.Id;
            var node = result.NodeId;
            var relevance = $"{result.Relevance:P0}"; // Format as percentage

            // Display title if available, otherwise truncated content
            var preview = !string.IsNullOrEmpty(result.Title)
                ? result.Title
                : result.Content.Length > 50
                    ? result.Content[..50] + "..."
                    : result.Content;

            table.AddRow(
                id,
                node,
                relevance,
                preview.Replace("[", "[[").Replace("]", "]]") // Escape markup
            );
        }

        AnsiConsole.Write(table);

        // Display metadata
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[dim]Total results: {response.TotalResults}[/]");
        AnsiConsole.MarkupLine($"[dim]Execution time: {response.Metadata.ExecutionTime.TotalMilliseconds:F0}ms[/]");
        AnsiConsole.MarkupLine($"[dim]Nodes searched: {response.Metadata.NodesSearched}/{response.Metadata.NodesRequested}[/]");

        if (response.Metadata.Warnings.Length > 0)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[yellow]Warnings:[/]");
            foreach (var warning in response.Metadata.Warnings)
            {
                AnsiConsole.MarkupLine($"  [yellow]⚠ {warning}[/]");
            }
        }

        // Show pagination info
        if (response.TotalResults > settings.Limit)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[dim]Showing results {settings.Offset + 1}-{Math.Min(settings.Offset + settings.Limit, response.TotalResults)}[/]");
            AnsiConsole.MarkupLine("[dim]Use --offset and --limit for pagination[/]");
        }
    }

    /// <summary>
    /// Creates a SearchService instance with all configured nodes.
    /// </summary>
    /// <returns>A configured SearchService.</returns>
    private SearchService CreateSearchService()
    {
        var nodeServices = new Dictionary<string, NodeSearchService>();

        foreach (var (nodeId, nodeConfig) in this.Config.Nodes)
        {
            // Create ContentService for this node
            using var contentService = this.CreateContentService(nodeConfig, readonlyMode: true);

            // Get FTS index from search indexes
            var ftsIndex = Services.SearchIndexFactory.CreateFtsIndex(nodeConfig.SearchIndexes);
            if (ftsIndex == null)
            {
                throw new InvalidOperationException($"Node '{nodeId}' does not have an FTS index configured");
            }

            // Create NodeSearchService
            var nodeSearchService = new NodeSearchService(
                nodeId,
                ftsIndex,
                contentService.Storage
            );

            nodeServices[nodeId] = nodeSearchService;
        }

        return new SearchService(nodeServices);
    }

    /// <summary>
    /// Shows a friendly first-run message when no database exists yet.
    /// </summary>
    /// <param name="settings">The command settings.</param>
    private void ShowFirstRunMessage(SearchCommandSettings settings)
    {
        var formatter = CLI.OutputFormatters.OutputFormatterFactory.Create(settings);

        if (!settings.Format.Equals("human", StringComparison.OrdinalIgnoreCase))
        {
            // Return empty search response for JSON/YAML
            var emptyResponse = new SearchResponse
            {
                Query = settings.Query,
                TotalResults = 0,
                Results = [],
                Metadata = new SearchMetadata
                {
                    NodesSearched = 0,
                    NodesRequested = 0,
                    ExecutionTime = TimeSpan.Zero,
                    NodeTimings = [],
                    Warnings = ["No database found - this is your first run"]
                }
            };
            formatter.Format(emptyResponse);
            return;
        }

        // Human format: friendly message
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold green]Welcome to Kernel Memory! 🚀[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]No content found yet. This is your first run.[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]To get started:[/]");
        AnsiConsole.MarkupLine("  [cyan]km put \"Your content here\"[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Then search:[/]");
        AnsiConsole.MarkupLine("  [cyan]km search \"your query\"[/]");
        AnsiConsole.WriteLine();
    }
}
