// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory.DataFormats;
using Microsoft.KernelMemory.DataFormats.Office;
using Microsoft.KernelMemory.DataFormats.Pdf;

FileContent content = new();

// ===================================================================================================================
// MS Word example
Console.WriteLine("===============================");
Console.WriteLine("=== Text in mswordfile.docx ===");
Console.WriteLine("===============================");

content = new MsWordDecoder().ExtractContent("mswordfile.docx");
foreach (FileSection section in content.Sections)
{
    Console.WriteLine($"Page: {section.Number}/{content.Sections.Count}");
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

content = new MsPowerPointDecoder().ExtractContent("mspowerpointfile.pptx",
    withSlideNumber: true,
    withEndOfSlideMarker: false,
    skipHiddenSlides: true);
foreach (FileSection section in content.Sections)
{
    Console.WriteLine($"Slide: {section.Number}/{content.Sections.Count}");
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

content = new MsExcelDecoder().ExtractContent("msexcelfile.xlsx");
foreach (FileSection section in content.Sections)
{
    Console.WriteLine($"Worksheet: {section.Number}/{content.Sections.Count}");
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

content = new PdfDecoder().ExtractContent("file1.pdf");
foreach (FileSection section in content.Sections)
{
    Console.WriteLine($"Page: {section.Number}/{content.Sections.Count}");
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

content = new PdfDecoder().ExtractContent("file2.pdf");
foreach (FileSection section in content.Sections)
{
    Console.WriteLine($"Page: {section.Number}/{content.Sections.Count}");
    Console.WriteLine(section.Content);
    Console.WriteLine("-----");
}
