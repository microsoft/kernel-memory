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
    //.WithOpenAIDefaults(Environment.GetEnvironmentVariable("OPENAI_API_KEY"))
    //.WithOpenAI(openAIConfig)
    .WithAzureOpenAITextGeneration(azureOpenAITextConfig)
    .WithAzureOpenAITextEmbeddingGeneration(azureOpenAIEmbeddingConfig)
    .With(new MsExcelDecoderConfig { BlankCellValue = "NO-VALUE" }) // Customize the default Excel decoder
    .With(new MsPowerPointDecoderConfig { SkipHiddenSlides = false, WithSlideNumber = true }) // Customize the default PowerPoint decoder
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

    public CustomPdfDecoder(ILoggerFactory? loggerFactory = null)
    {
        this._log = (loggerFactory ?? DefaultLogger.Factory).CreateLogger<CustomPdfDecoder>();
    }

    /// <inheritdoc />
    public bool SupportsMimeType(string mimeType)
    {
        return mimeType != null && mimeType.StartsWith(MimeTypes.Pdf, StringComparison.OrdinalIgnoreCase);
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
        this._log.LogDebug("Extracting text from PDF file");

        var result = new FileContent(MimeTypes.PlainText);

        using PdfDocument? pdfDocument = PdfDocument.Open(data);
        if (pdfDocument == null) { return Task.FromResult(result); }

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

        return Task.FromResult(result);
    }
}
