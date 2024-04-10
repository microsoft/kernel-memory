// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.DataFormats;
using Microsoft.KernelMemory.DataFormats.Office;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.Pipeline;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

var openAIConfig = new OpenAIConfig();
var azureOpenAITextConfig = new AzureOpenAIConfig();
var azureOpenAIEmbeddingConfig = new AzureOpenAIConfig();

new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddJsonFile("appsettings.Development.json", optional: true)
    .Build()
    .BindSection("KernelMemory:Services:OpenAI", openAIConfig)
    .BindSection("KernelMemory:Services:AzureOpenAIText", azureOpenAITextConfig)
    .BindSection("KernelMemory:Services:AzureOpenAIEmbedding", azureOpenAIEmbeddingConfig);

var memory = new KernelMemoryBuilder()
    //.WithOpenAIDefaults(Env.Var("OPENAI_API_KEY"))
    //.WithOpenAI(openAIConfig)
    .WithAzureOpenAITextGeneration(azureOpenAITextConfig)
    .WithAzureOpenAITextEmbeddingGeneration(azureOpenAIEmbeddingConfig)
    .With(new MsExcelConfig { BlankCellValue = "NO-VALUE" })    // Customize the default Excel decoder
    .WithContentDecoder<CustomPdfDecoder>() // Register a custom PDF decoder
    .Build<MemoryServerless>();

await memory.ImportDocumentAsync("file5-NASA-news.pdf");

// Check to see if the importing has been successful.
var question = "Any news from NASA about Orion?";
Console.WriteLine($"Question: {question}");

var answer = await memory.AskAsync(question);
Console.WriteLine($"\nAnswer: {answer.Result}");

public class CustomPdfDecoder : IContentDecoder
{
    private readonly ILogger<CustomPdfDecoder> _log;

    public IEnumerable<string> SupportedMimeTypes => [MimeTypes.Pdf];

    public CustomPdfDecoder(ILogger<CustomPdfDecoder>? log = null)
    {
        this._log = log ?? DefaultLogger<CustomPdfDecoder>.Instance;
    }

    public Task<FileContent?> ExtractContentAsync(string handlerStepName, DataPipeline.FileDetails file, string filename, CancellationToken cancellationToken = default)
    {
        using var stream = File.OpenRead(filename);
        return this.ExtractContentAsync(handlerStepName, file, stream, cancellationToken);
    }

    public Task<FileContent?> ExtractContentAsync(string handlerStepName, DataPipeline.FileDetails file, BinaryData data, CancellationToken cancellationToken = default)
    {
        using var stream = data.ToStream();
        return this.ExtractContentAsync(handlerStepName, file, stream, cancellationToken);
    }

    public Task<FileContent?> ExtractContentAsync(string handlerStepName, DataPipeline.FileDetails file, Stream data, CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Extracting text from PDF file {0}", file.Name);

        var result = new FileContent();

        using PdfDocument? pdfDocument = PdfDocument.Open(data);
        if (pdfDocument == null) { return Task.FromResult(result)!; }

        var options = new ContentOrderTextExtractor.Options
        {
            ReplaceWhitespaceWithSpace = true,
            SeparateParagraphsWithDoubleNewline = false,
        };

        foreach (Page? page in pdfDocument.GetPages().Where(x => x != null))
        {
            string pageContent = (ContentOrderTextExtractor.GetText(page, options) ?? string.Empty).ReplaceLineEndings(" ");
            result.Sections.Add(new FileSection(page.Number, pageContent, false));
        }

        return Task.FromResult(result)!;
    }
}
