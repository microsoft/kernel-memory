// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Services;
using SemanticKernel.Data.Nl2Sql.Library;
using SemanticKernel.Data.Nl2Sql.Library.Schema;

namespace SemanticKernel.Data.Nl2Sql;

internal sealed class Nl2SqlConsole : BackgroundService
{
    private const ConsoleColor ErrorColor = ConsoleColor.Magenta;
    private const ConsoleColor FocusColor = ConsoleColor.Yellow;
    private const ConsoleColor QueryColor = ConsoleColor.Green;
    private const ConsoleColor SystemColor = ConsoleColor.Cyan;

    private static readonly Dictionary<Type, int> s_typeWidths =
        new()
        {
            { typeof(bool), 5 },
            { typeof(int), 9 },
            { typeof(DateTime), 12 },
            { typeof(TimeSpan), 12 },
            { typeof(Guid), 8 },
        };

    private readonly Kernel _kernel;
    private readonly ISemanticTextMemory _memory;
    private readonly SqlConnectionProvider _sqlProvider;
    private readonly SqlQueryGenerator _queryGenerator;
    private readonly ILogger<Nl2SqlConsole> _logger;

    public Nl2SqlConsole(
        Kernel kernel,
        ISemanticTextMemory memory,
        IConfiguration config,
        SqlConnectionProvider sqlProvider,
        ILogger<Nl2SqlConsole> logger)
    {
        var minRelevance = config.GetValue<double>("MinSchemaRelevance", SqlQueryGenerator.DefaultMinRelevance);

        this._kernel = kernel;
        this._memory = memory;
        this._sqlProvider = sqlProvider;
        this._logger = logger;
        this._queryGenerator = new SqlQueryGenerator(this._kernel, this._memory, Repo.RootConfigFolder, minRelevance);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield(); // Yield to hosting framework prior to blocking on console input

        try
        {
            await this.ExecuteConsoleAsync(stoppingToken); // Process console UX
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            this.WriteLine(ErrorColor, "Unexpected failure!");
            this.WriteLine(ErrorColor, exception.ToString());
        }
    }

    private async Task ExecuteConsoleAsync(CancellationToken stoppingToken)
    {
        var schemaNames = SchemaDefinitions.GetNames().ToArray();
        await SchemaProvider.InitializeAsync(
            this._memory,
            schemaNames.Select(s => Path.Combine(Repo.RootConfigFolder, "schema", $"{s}.json"))).ConfigureAwait(false);

        this.WriteIntroduction(schemaNames);

        while (!stoppingToken.IsCancellationRequested)
        {
            var objective = await ReadInputAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(objective))
            {
                continue;
            }

            var result =
                await this._queryGenerator.SolveObjectiveAsync(objective).ConfigureAwait(false);

            await ProcessQueryAsync(result).ConfigureAwait(false);
        }

        this.WriteLine();

        // Capture console input with cancellation detection
        async Task<string?> ReadInputAsync()
        {
            this.Write(SystemColor, "# ");

            var inputTask = Console.In.ReadLineAsync(stoppingToken).AsTask();
            var objective = await inputTask.ConfigureAwait(false);

            // Null response occurs when blocking input is cancelled (CTRL+C)
            if (null == objective)
            {
                this.WriteLine();
                this.WriteLine(FocusColor, "Cancellation detected...");

                // Yield to sync stoppingToken state
                await Task.Delay(TimeSpan.FromMilliseconds(300), stoppingToken).ConfigureAwait(false);
            }
            else if (string.IsNullOrWhiteSpace(objective))
            {
                this.WriteLine(FocusColor, $"Please provide a query related to the defined schemas.{Environment.NewLine}");
            }
            else
            {
                this.ClearLine(previous: true);
                this.WriteLine(QueryColor, $"# {objective}");
            }

            return objective;
        }

        // Display query result and (optionally) execute.
        async Task ProcessQueryAsync(SqlQueryResult? result)
        {
            if (result == null)
            {
                this.WriteLine(FocusColor, $"Unable to translate request into a query.{Environment.NewLine}");
                return;
            }

            this.WriteLine(SystemColor, $"{Environment.NewLine}SCHEMA:");
            this.WriteLine(QueryColor, result.Schema);
            this.WriteLine(SystemColor, $"{Environment.NewLine}QUERY:");
            this.WriteLine(QueryColor, result.Query);

            if (!this.Confirm($"{Environment.NewLine}Execute?"))
            {
                this.WriteLine();
                this.WriteLine();
                return;
            }

            await Task.Delay(300, stoppingToken).ConfigureAwait(false); // Human feedback window

            this.ClearLine();
            this.Write(SystemColor, "Executing...");

            await ProcessDataAsync(
                result.Schema,
                result.Query,
                reader =>
                {
                    this.ClearLine();
                    this.WriteData(reader);
                }).ConfigureAwait(false);
        }

        // Execute query and display the resulting data-set.
        async Task ProcessDataAsync(string schema, string query, Action<IDataReader> callback)
        {
            try
            {
                using var connection = await this._sqlProvider.ConnectAsync(schema).ConfigureAwait(false);
                using var command = connection.CreateCommand();

#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
                command.CommandText = query;
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

                using var reader = await command.ExecuteReaderAsync(stoppingToken).ConfigureAwait(false);
                callback.Invoke(reader);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception exception)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                this.ClearLine();
                this.WriteLine(FocusColor, exception.Message);
            }
        }
    }

    /// <summary>
    /// Render a data-reader to the console, in pages.
    /// </summary>
    private void WriteData(IDataReader reader)
    {
        int maxPage = Console.WindowHeight - 10;

        var widths = GetWidths().ToArray();
        var isColumnTruncation = widths.Length < reader.FieldCount;
        var rowFormatter = string.Join('│', widths.Select((width, index) => width == -1 ? $"{{{index}}}" : $"{{{index},-{width}}}"));

        if (isColumnTruncation)
        {
            rowFormatter = string.Concat(rowFormatter, isColumnTruncation ? $"│{{{widths.Length}}}" : string.Empty);
        }

        WriteRow(GetColumns());

        WriteSeparator(widths);

        bool showData;

        do
        {
            int count = 0;
            while (reader.Read() && count < maxPage)
            {
                WriteRow(GetValues());

                count++;
            }

            if (count >= maxPage)
            {
                showData = this.Confirm($"...More?");
                this.ClearLine();
                if (!showData)
                {
                    this.WriteLine();
                }
            }
            else
            {
                showData = false;
                this.WriteLine();
            }
        } while (showData);

        void WriteRow(IEnumerable<string> fields)
        {
            fields = TrimValues(fields).Concat(isColumnTruncation ? new[] { "..." } : Array.Empty<string>());

            this.WriteLine(SystemColor, rowFormatter, fields.ToArray());
        }

        IEnumerable<string> TrimValues(IEnumerable<string> fields)
        {
            int index = 0;
            int totalWidth = 0;

            foreach (var field in fields)
            {
                if (index >= widths.Length)
                {
                    yield break;
                }

                var width = widths[index];
                ++index;

                if (width == -1)
                {
                    var remainingWidth = Console.WindowWidth - totalWidth;

                    yield return TrimValue(field, remainingWidth);
                    yield break;
                }

                totalWidth += width + 1;

                yield return TrimValue(field, width);
            }
        }

        string TrimValue(string? value, int width)
        {
            value ??= string.Empty;

            if (value.Length <= width)
            {
                return value;
            }

            return string.Concat(value.AsSpan(0, width - 4), "...");
        }

        void WriteSeparator(int[] widths)
        {
            int totalWidth = 0;

            for (int index = 0; index < widths.Length; index++)
            {
                if (index > 0)
                {
                    this.Write(SystemColor, "┼");
                }

                var width = widths[index];

                this.Write(SystemColor, new string('─', width == -1 ? Console.WindowWidth - totalWidth : width));

                totalWidth += width + 1;
            }

            if (isColumnTruncation)
            {
                this.Write(SystemColor, "┼───");
            }

            this.WriteLine();
        }

        IEnumerable<int> GetWidths()
        {
            if (reader.FieldCount == 1)
            {
                yield return -1;
                yield break;
            }

            int totalWidth = 0;

            for (int index = 0; index < reader.FieldCount; ++index)
            {
                if (index == reader.FieldCount - 1)
                {
                    // Last field gets remaining width
                    yield return -1;
                    yield break;
                }

                var width = GetWidth(reader.GetFieldType(index));

                if (totalWidth + width > Console.WindowWidth - 11)
                {
                    yield break;
                }

                totalWidth += width;

                yield return width;
            }
        }

        static int GetWidth(Type type)
        {
            if (!s_typeWidths.TryGetValue(type, out var width))
            {
                return 16; // Default width
            }

            return width;
        }

        IEnumerable<string> GetColumns()
        {
            for (int index = 0; index < reader.FieldCount; ++index)
            {
                var label = reader.GetName(index);

                yield return string.IsNullOrWhiteSpace(label) ? $"#{index + 1}" : label;
            }
        }

        IEnumerable<string> GetValues()
        {
            for (int index = 0; index < reader.FieldCount; ++index)
            {
                yield return reader.GetValue(index)?.ToString() ?? string.Empty;
            }
        }
    }

    // Display the introduction when the app-starts.
    private void WriteIntroduction(IList<string> schemaNames)
    {
        this.WriteLine(SystemColor, $"I can translate your question into a SQL query for the following data schemas:{Environment.NewLine}");
        this.WriteLine(SystemColor, $"Model: {this._kernel.GetRequiredService<IChatCompletionService>().GetModelId()}{Environment.NewLine}");

        foreach (var schemaName in schemaNames)
        {
            this.WriteLine(SystemColor, $"- {schemaName}");
        }

        this.WriteLine(SystemColor, $"{Environment.NewLine}Press CTRL+C to Exit.{Environment.NewLine}");
    }

    // WriteLine to the console with the specified color
    private void WriteLine(ConsoleColor? color = null, string? message = null, params string[] args)
    {
        this.Write(color ?? Console.ForegroundColor, message ?? string.Empty, args);

        Console.WriteLine();
    }

    // Write to the console with the specified color
    private void Write(ConsoleColor color, string message, params string[] args)
    {
        var currentColor = Console.ForegroundColor;

        try
        {
            Console.ForegroundColor = color;
            if (args.Length == 0)
            {
                Console.Write(message);
            }
            else
            {
                Console.Write(message, args);
            }
        }
        finally
        {
            Console.ForegroundColor = currentColor;
        }
    }

    // Clear the current console line so that it may be over-written.
    private void ClearLine(bool previous = false)
    {
        if (previous)
        {
            --Console.CursorTop;
        }

        Console.CursorLeft = 0;
        Console.Write(new string(' ', Console.WindowWidth));
        Console.CursorLeft = 0;
    }

    /// <summary>
    /// Elicit a Y or N response.
    /// </summary>
    private bool Confirm(string message)
    {
        this.Write(FocusColor, $"{message} (y/n) ");

        while (true)
        {
            var choice = Console.ReadKey(intercept: true);
            switch (char.ToUpperInvariant(choice.KeyChar))
            {
                case 'N':
                    this.Write(FocusColor, "N");
                    return false;
                case 'Y':
                    this.Write(FocusColor, "Y");
                    return true;
                default:
                    break;
            }
        }
    }
}
