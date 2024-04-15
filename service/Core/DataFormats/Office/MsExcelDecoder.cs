// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.Pipeline;

namespace Microsoft.KernelMemory.DataFormats.Office;

public class MsExcelDecoder : IContentDecoder
{
    private readonly MsExcelDecoderConfig _config;
    private readonly ILogger<MsExcelDecoder> _log;

    public MsExcelDecoder(MsExcelDecoderConfig? config = null, ILogger<MsExcelDecoder>? log = null)
    {
        this._config = config ?? new MsExcelDecoderConfig();
        this._log = log ?? DefaultLogger<MsExcelDecoder>.Instance;
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
                sb.AppendLine(this._config.WorksheetNumberTemplate.Replace("{number}", $"{worksheetNumber}", StringComparison.OrdinalIgnoreCase));
            }

            foreach (IXLRangeRow? row in worksheet.RangeUsed().RowsUsed())
            {
                if (row == null) { continue; }

                var cells = row.Cells().ToList();

                sb.Append(this._config.RowPrefix);
                for (var i = 0; i < cells.Count; i++)
                {
                    IXLCell? cell = cells[i];

                    if (this._config.WithQuotes && cell is { Value.IsText: true })
                    {
                        sb.Append('"')
                            .Append(cell.Value.GetText().Replace("\"", "\"\"", StringComparison.Ordinal))
                            .Append('"');
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

                sb.AppendLine(this._config.RowSuffix);
            }

            if (this._config.WithEndOfWorksheetMarker)
            {
                sb.AppendLine(this._config.EndOfWorksheetMarkerTemplate.Replace("{number}", $"{worksheetNumber}", StringComparison.OrdinalIgnoreCase));
            }

            string worksheetContent = sb.ToString().Trim();
            sb.Clear();
            result.Sections.Add(new FileSection(worksheetNumber, worksheetContent, true));
        }

        return Task.FromResult(result);
    }
}
