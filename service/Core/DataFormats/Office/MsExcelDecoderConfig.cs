// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.KernelMemory.DataFormats.Office;

public class MsExcelDecoderConfig
{
    public bool WithWorksheetNumber { get; set; } = true;

    public bool WithEndOfWorksheetMarker { get; set; } = false;

    public bool WithQuotes { get; set; } = true;

    public string WorksheetNumberTemplate { get; set; } = "\n# Worksheet {number}\n";

    public string EndOfWorksheetMarkerTemplate { get; set; } = "\n# End of worksheet {number}";

    public string RowPrefix { get; set; } = string.Empty;

    public string ColumnSeparator { get; set; } = string.Empty;

    public string RowSuffix { get; set; } = string.Empty;

    public string BlankCellValue { get; set; } = string.Empty;
}
