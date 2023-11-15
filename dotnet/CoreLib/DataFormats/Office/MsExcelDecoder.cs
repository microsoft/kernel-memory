// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ClosedXML.Excel;

namespace Microsoft.KernelMemory.DataFormats.Office;

public class MsExcelDecoder
{
    public string DocToText(string filename)
    {
        using var stream = File.OpenRead(filename);
        return this.DocToText(stream);
    }

    public string DocToText(BinaryData data)
    {
        using var stream = data.ToStream();
        return this.DocToText(stream);
    }

    public string DocToText(Stream data)
    {
        using var workbook = new XLWorkbook(data);
        var sb = new StringBuilder();

        foreach (var worksheet in workbook.Worksheets)
        {
            var range = worksheet.RangeUsed();
            var rowsUsed = range.RowsUsed().ToList();

            foreach (var row in rowsUsed)
            {
                var cellsUsed = row.CellsUsed().ToList();

                for (var i = 0; i < cellsUsed.Count; i++)
                {
                    var cell = cellsUsed[i];
                    var cellValue = cell.Value.ToString(CultureInfo.CurrentCulture);

                    sb.Append(cell.Value);

                    if (i < cellsUsed.Count - 1)
                    {
                        sb.Append(' ');
                    }
                }

                sb.AppendLine();
            }
        }

        return sb.ToString().Trim();
    }
}
