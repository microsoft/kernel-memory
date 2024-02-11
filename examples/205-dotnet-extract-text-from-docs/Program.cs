// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory.DataFormats;
using Microsoft.KernelMemory.DataFormats.Office;
using Microsoft.KernelMemory.DataFormats.Pdf;

List<FileSection> sections = new();

// ===================================================================================================================
// MS Word example
Console.WriteLine("===============================");
Console.WriteLine("=== Text in mswordfile.docx ===");
Console.WriteLine("===============================");

sections = new MsWordDecoder().DocToText("mswordfile.docx");
foreach (FileSection section in sections)
{
    Console.WriteLine($"Page: {section.Number}/{sections.Count}");
    Console.WriteLine(section.Content);
    Console.WriteLine("-----");
}

Console.WriteLine("============================");
Console.WriteLine("Press a Enter to continue...");
Console.ReadLine();

// ===================================================================================================================
// MS PowerPoint example
Console.WriteLine("===============================");
Console.WriteLine("=== Text in mspowerpointfile.pptx ===");
Console.WriteLine("===============================");

sections = new MsPowerPointDecoder().DocToText("mspowerpointfile.pptx",
    withSlideNumber: true,
    withEndOfSlideMarker: false,
    skipHiddenSlides: true);
foreach (FileSection section in sections)
{
    Console.WriteLine($"Slide: {section.Number}/{sections.Count}");
    Console.WriteLine(section.Content);
    Console.WriteLine("-----");
}

Console.WriteLine("============================");
Console.WriteLine("Press a Enter to continue...");
Console.ReadLine();

// ===================================================================================================================
// MS Excel example
Console.WriteLine("===============================");
Console.WriteLine("=== Text in msexcelfile.xlsx ===");
Console.WriteLine("===============================");

sections = new MsExcelDecoder().DocToText("msexcelfile.xlsx");
foreach (FileSection section in sections)
{
    Console.WriteLine($"Worksheet: {section.Number}/{sections.Count}");
    Console.WriteLine(section.Content);
    Console.WriteLine("-----");
}

Console.WriteLine("============================");
Console.WriteLine("Press a Enter to continue...");
Console.ReadLine();

// ===================================================================================================================
// PDF example 1, short document
Console.WriteLine("=========================");
Console.WriteLine("=== Text in file1.pdf ===");
Console.WriteLine("=========================");

sections = new PdfDecoder().DocToText("file1.pdf");
foreach (FileSection section in sections)
{
    Console.WriteLine($"Page: {section.Number}/{sections.Count}");
    Console.WriteLine(section.Content);
    Console.WriteLine("-----");
}

Console.WriteLine("============================");
Console.WriteLine("Press a Enter to continue...");
Console.ReadLine();

// ===================================================================================================================
// PDF example 2, scanned book
Console.WriteLine("=========================");
Console.WriteLine("=== Text in file2.pdf ===");
Console.WriteLine("=========================");

sections = new PdfDecoder().DocToText("file2.pdf");
foreach (FileSection section in sections)
{
    Console.WriteLine($"Page: {section.Number}/{sections.Count}");
    Console.WriteLine(section.Content);
    Console.WriteLine("-----");
}
