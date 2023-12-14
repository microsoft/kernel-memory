// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory.DataFormats.Office;
using Microsoft.KernelMemory.DataFormats.Pdf;

// MS Word example
Console.WriteLine("===============================");
Console.WriteLine("=== Text in mswordfile.docx ===");
Console.WriteLine("===============================");

var text = new MsWordDecoder().DocToText("mswordfile.docx");
Console.WriteLine(text);

Console.WriteLine("============================");
Console.WriteLine("Press a Enter to continue...");
Console.ReadLine();

// MS PowerPoint example
Console.WriteLine("===============================");
Console.WriteLine("=== Text in mspowerpointfile.pptx ===");
Console.WriteLine("===============================");

text = new MsPowerPointDecoder().DocToText("mspowerpointfile.pptx",
    withSlideNumber: true,
    withEndOfSlideMarker: false,
    skipHiddenSlides: true);
Console.WriteLine(text);

Console.WriteLine("============================");
Console.WriteLine("Press a Enter to continue...");
Console.ReadLine();

// MS Excel example
Console.WriteLine("===============================");
Console.WriteLine("=== Text in msexcelfile.xlsx ===");
Console.WriteLine("===============================");

text = new MsExcelDecoder().DocToText("msexcelfile.xlsx");
Console.WriteLine(text);

Console.WriteLine("============================");
Console.WriteLine("Press a Enter to continue...");
Console.ReadLine();

// PDF example 1, short document
Console.WriteLine("=========================");
Console.WriteLine("=== Text in file1.pdf ===");
Console.WriteLine("=========================");

var pages = new PdfDecoder().DocToText("file1.pdf");
foreach (var page in pages)
{
    Console.WriteLine(page.Text);
}

Console.WriteLine("============================");
Console.WriteLine("Press a Enter to continue...");
Console.ReadLine();

// PDF example 2, scanned book
Console.WriteLine("=========================");
Console.WriteLine("=== Text in file2.pdf ===");
Console.WriteLine("=========================");

pages = new PdfDecoder().DocToText("file2.pdf");
foreach (var page in pages)
{
    Console.WriteLine(page.Text);
}
