// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Globalization;

namespace Microsoft.KernelMemory.DataFormats.Office;

public class MsExcelDecoderConfig
{
    public bool WithWorksheetNumber { get; set; } = true;
    public bool WithEndOfWorksheetMarker { get; set; } = false;
    public bool WithQuotes { get; set; } = true;
    public string WorksheetNumberTemplate { get; set; } = "\n# Worksheet {number}\n";
    public string EndOfWorksheetMarkerTemplate { get; set; } = "\n# End of worksheet {number}";
    public string RowPrefix { get; set; } = string.Empty;
    public string ColumnSeparator { get; set; } = ", ";
    public string RowSuffix { get; set; } = string.Empty;
    public string BlankCellValue { get; set; } = string.Empty;
    public string BooleanTrueValue { get; set; } = "TRUE";
    public string BooleanFalseValue { get; set; } = "FALSE";
    public string TimeSpanFormat { get; set; } = "g";
    public IFormatProvider TimeSpanProvider { get; set; } = CultureInfo.CurrentCulture;
    public string DateFormat { get; set; } = "d";
    public IFormatProvider DateFormatProvider { get; set; } = CultureInfo.CurrentCulture;
}
