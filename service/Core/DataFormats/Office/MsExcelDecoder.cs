// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.Pipeline;
using Microsoft.KernelMemory.Text;

namespace Microsoft.KernelMemory.DataFormats.Office;

[Experimental("KMEXP00")]
public sealed class MsExcelDecoder : IContentDecoder
{
    private readonly MsExcelDecoderConfig _config;
    private readonly ILogger<MsExcelDecoder> _log;

    public MsExcelDecoder(MsExcelDecoderConfig? config = null, ILoggerFactory? loggerFactory = null)
    {
        this._config = config ?? new MsExcelDecoderConfig();
        this._log = (loggerFactory ?? DefaultLogger.Factory).CreateLogger<MsExcelDecoder>();
    }

    /// <inheritdoc />
    public bool SupportsMimeType(string mimeType)
    {
        return mimeType != null && mimeType.StartsWith(MimeTypes.MsExcelX, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public Task<FileContent> DecodeAsync(string filename, CancellationToken cancellationToken = default)
    {
        using var stream = File.OpenRead(filename);
        return this.DecodeAsync(stream, cancellationToken);
    }

    /// <inheritdoc />
    public Task<FileContent> DecodeAsync(BinaryData data, CancellationToken cancellationToken = default)
    {
        using var stream = data.ToStream();
        return this.DecodeAsync(stream, cancellationToken);
    }

    /// <inheritdoc />
    public Task<FileContent> DecodeAsync(Stream data, CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Extracting text from MS Excel file");

        var result = new FileContent(MimeTypes.PlainText);
        using var workbook = new XLWorkbook(data);
        var sb = new StringBuilder();

        var worksheetNumber = 0;
        foreach (var worksheet in workbook.Worksheets)
        {
            worksheetNumber++;
            if (this._config.WithWorksheetNumber)
            {
                sb.AppendLineNix(this._config.WorksheetNumberTemplate.Replace("{number}", $"{worksheetNumber}", StringComparison.OrdinalIgnoreCase));
            }

            var rowsUsed = worksheet.RangeUsed()?.RowsUsed();
            if (rowsUsed == null)
            {
                continue;
            }

            foreach (IXLRangeRow? row in rowsUsed)
            {
                if (row == null) { continue; }

                var cells = row.Cells().ToList();

                sb.Append(this._config.RowPrefix);
                for (var i = 0; i < cells.Count; i++)
                {
                    IXLCell? cell = cells[i];

                    /* Note: some data types are not well supported; for example the values below
                     *       are extracted incorrectly regardless of the cell configuration.
                     *       In this cases using Text cell type might be better.
                     *
                     * - Date: "Monday, December 25, 2090"  => "69757"
                     * - Time: "12:55:00"                   => "0.5381944444444444"
                     * - Time: "12:55"                      => "12/31/1899"
                     * - Currency symbols are not extracted
                     */
                    if (this._config.WithQuotes)
                    {
                        sb.Append('"');
                        if (cell == null || cell.Value.IsBlank)
                        {
                            sb.Append(this._config.BlankCellValue);
                        }
                        else if (cell.Value.IsTimeSpan)
                        {
                            sb.Append(cell.Value.GetTimeSpan().ToString(this._config.TimeSpanFormat, this._config.TimeSpanProvider));
                        }
                        else if (cell.Value.IsDateTime)
                        {
                            // TODO: check cell.Style.DateFormat.Format
                            sb.Append(cell.Value.GetDateTime().ToString(this._config.DateFormat, this._config.DateFormatProvider));
                        }
                        else if (cell.Value.IsBoolean)
                        {
                            sb.Append(cell.Value.GetBoolean() ? this._config.BooleanTrueValue : this._config.BooleanFalseValue);
                        }
                        else if (cell.Value.IsText)
                        {
                            var value = cell.Value.GetText().Replace("\"", "\"\"", StringComparison.Ordinal);
                            sb.Append(string.IsNullOrEmpty(value) ? this._config.BlankCellValue : value);
                        }
                        else if (cell.Value.IsNumber)
                        {
                            // TODO: check cell.Style.NumberFormat.Format and cell.Style.DateFormat.Format to detect dates, currency symbols, phone numbers
                            sb.Append(cell.Value.GetNumber());
                        }
                        else if (cell.Value.IsUnifiedNumber)
                        {
                            sb.Append(cell.Value.GetUnifiedNumber());
                        }
                        else if (cell.Value.IsError)
                        {
                            sb.Append(cell.Value.GetError().ToString().Replace("\"", "\"\"", StringComparison.Ordinal));
                        }

                        sb.Append('"');
                    }
                    else
                    {
                        sb.Append(cell.Value.IsBlank ? this._config.BlankCellValue : cell.Value);
                    }

                    if (i < cells.Count - 1)
                    {
                        sb.Append(this._config.ColumnSeparator);
                    }
                }

                sb.AppendLineNix(this._config.RowSuffix);
            }

            if (this._config.WithEndOfWorksheetMarker)
            {
                sb.AppendLineNix(this._config.EndOfWorksheetMarkerTemplate.Replace("{number}", $"{worksheetNumber}", StringComparison.OrdinalIgnoreCase));
            }

            string worksheetContent = sb.ToString().NormalizeNewlines(true);
            sb.Clear();
            result.Sections.Add(new Chunk(worksheetContent, worksheetNumber, Chunk.Meta(sentencesAreComplete: true)));
        }

        return Task.FromResult(result);
    }
}
