// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory.DataFormats;
using Microsoft.KernelMemory.DataFormats.Image;
using Microsoft.KernelMemory.DataFormats.Office;
using Microsoft.KernelMemory.DataFormats.Pdf;
using Microsoft.KernelMemory.DataFormats.Text;
using Microsoft.KernelMemory.DataFormats.WebPages;
using Microsoft.KernelMemory.Pipeline;
using Microsoft.KernelMemory.Prompts;

namespace Microsoft.KernelMemory;

/// <summary>
/// Kernel Memory builder extensions.
/// </summary>
public static partial class KernelMemoryBuilderExtensions
{
    public static IKernelMemoryBuilder WithDefaultMimeTypeDetection(
        this IKernelMemoryBuilder builder)
    {
        builder.AddSingleton<IMimeTypeDetection, MimeTypesDetection>();
        return builder;
    }

    public static IKernelMemoryBuilder WithDefaultPromptProvider(
        this IKernelMemoryBuilder builder)
    {
        builder.AddSingleton<IPromptProvider, EmbeddedPromptProvider>();
        return builder;
    }

    public static IKernelMemoryBuilder WithDefaultContentDecoders(
        this IKernelMemoryBuilder builder)
    {
        builder.AddSingleton<IContentDecoder, ImageDecoder>();
        builder.AddSingleton<IContentDecoder, MsExcelDecoder>();
        builder.AddSingleton<IContentDecoder, MsPowerPointDecoder>();
        builder.AddSingleton<IContentDecoder, MsWordDecoder>();
        builder.AddSingleton<IContentDecoder, PdfDecoder>();
        builder.AddSingleton<IContentDecoder, TextDecoder>();
        builder.AddSingleton<IContentDecoder, HtmlDecoder>();
        builder.AddSingleton<IContentDecoder, WebScraperDecoder>();

        return builder;
    }
}
