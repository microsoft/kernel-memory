// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory.DataFormats;
using Microsoft.KernelMemory.DataFormats.Office;
using Microsoft.KernelMemory.DataFormats.Pdf;
using Microsoft.KernelMemory.Pipeline;

FileContent? content = new();

// ===================================================================================================================
// MS Word example
Console.WriteLine("===============================");
Console.WriteLine("=== Text in mswordfile.docx ===");
Console.WriteLine("===============================");

var msWordWecoder = new MsWordDecoder();
content = await msWordWecoder.ExtractContentAsync("extract",
    new DataPipeline.FileDetails()
    {
        Name = "mswordfile.docx",
        MimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
    },
    "mswordfile.docx");

foreach (FileSection section in content!.Sections)
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

var msPowerPointDecoder = new MsPowerPointDecoder();
content = await msPowerPointDecoder.ExtractContentAsync("extract",
    new DataPipeline.FileDetails()
    {
        Name = "mspowerpointfile.pptx",
        MimeType = "application/vnd.openxmlformats-officedocument.presentationml.presentation"
    },
    "mspowerpointfile.pptx");

foreach (FileSection section in content!.Sections)
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

var msExcelDecoder = new MsExcelDecoder();
content = await msExcelDecoder.ExtractContentAsync("extract",
    new DataPipeline.FileDetails()
    {
        Name = "msexcelfile.pptx",
        MimeType = "application/vnd.openxmlformats-officedocument.presentationml.presentation"
    },
    "msexcelfile.xlsx");

foreach (FileSection section in content!.Sections)
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

var pdfDecoder = new PdfDecoder();
content = await pdfDecoder.ExtractContentAsync("extract",
    new DataPipeline.FileDetails()
    {
        Name = "file1.pdf",
        MimeType = "application/pdf"
    },
    "file1.pdf");

foreach (FileSection section in content!.Sections)
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

content = await pdfDecoder.ExtractContentAsync("extract",
    new DataPipeline.FileDetails()
    {
        Name = "file2.pdf",
        MimeType = "application/pdf"
    },
    "file2.pdf");

foreach (FileSection section in content!.Sections)
{
    Console.WriteLine($"Page: {section.Number}/{content.Sections.Count}");
    Console.WriteLine(section.Content);
    Console.WriteLine("-----");
}
