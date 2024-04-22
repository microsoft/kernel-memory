// Copyright (c) Microsoft. All rights reserved.

using System;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.DataFormats;
using Microsoft.KernelMemory.DataFormats.Image;
using Microsoft.KernelMemory.DataFormats.Office;
using Microsoft.KernelMemory.DataFormats.Pdf;
using Microsoft.KernelMemory.DataFormats.Text;
using Microsoft.KernelMemory.DataFormats.WebPages;

// ReSharper disable once CheckNamespace
namespace Microsoft.KernelMemory;

public static partial class KernelMemoryBuilderExtensions
{

    [Obsolete("This method is obsolete and will be removed in a future version. Use WithCustomContentDecoder<T> instead.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IKernelMemoryBuilder WithContentDecoder<T>(
        this IKernelMemoryBuilder builder) where T : class, IContentDecoder
    {
        return WithCustomContentDecoder<T>(builder);
    }

    [Obsolete("This method is obsolete and will be removed in a future version. Use WithCustomContentDecoder instead.")]
    [EditorBrowsable(EditorBrowsableState.Never)]

    public static IKernelMemoryBuilder WithContentDecoder(
        this IKernelMemoryBuilder builder, IContentDecoder decoder)
    {
        return WithCustomContentDecoder(builder, decoder);
    }

    public static IKernelMemoryBuilder WithCustomContentDecoder<T>(
        this IKernelMemoryBuilder builder) where T : class, IContentDecoder
    {
        builder.Services.AddCustomContentDecoder<T>();
        return builder;
    }

    public static IKernelMemoryBuilder WithCustomContentDecoder(
        this IKernelMemoryBuilder builder, IContentDecoder decoder)
    {
        builder.Services.AddCustomContentDecoder(decoder);
        return builder;
    }

    public static IKernelMemoryBuilder WithDefaultContentDecoders(
        this IKernelMemoryBuilder builder)
    {
        builder.Services.AddDefaultContentDecoders();
        return builder;
    }

    public static IKernelMemoryBuilder WithDefaultWebScraper(
        this IKernelMemoryBuilder builder)
    {
        builder.Services.AddDefaultWebScraper();
        return builder;
    }

    public static IKernelMemoryBuilder WithCustomWebScraper(
        this IKernelMemoryBuilder builder, IWebScraper webScraper)
    {
        builder.Services.AddCustomWebScraper(webScraper);
        return builder;
    }

    public static IKernelMemoryBuilder WithCustomWebScraper<T>(
        this IKernelMemoryBuilder builder) where T : class, IWebScraper
    {
        builder.Services.AddCustomWebScraper<T>();
        return builder;
    }
}

public static partial class DependencyInjection
{
    [Obsolete("This method is obsolete and will be removed in a future version. Use AddCustomContentDecoder<T> instead.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IServiceCollection AddContentDecoder<T>(
        this IServiceCollection services) where T : class, IContentDecoder
    {
        return AddCustomContentDecoder<T>(services);
    }

    [Obsolete("This method is obsolete and will be removed in a future version. Use AddCustomContentDecoder instead.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IServiceCollection AddContentDecoder(
        this IServiceCollection services, IContentDecoder decoder)
    {
        return AddCustomContentDecoder(services, decoder);
    }

    public static IServiceCollection AddCustomContentDecoder<T>(
        this IServiceCollection services) where T : class, IContentDecoder
    {
        services.AddSingleton<IContentDecoder, T>();
        return services;
    }

    public static IServiceCollection AddCustomContentDecoder(
        this IServiceCollection services, IContentDecoder decoder)
    {
        services.AddSingleton<IContentDecoder>(decoder);
        return services;
    }

    public static IServiceCollection AddDefaultContentDecoders(
        this IServiceCollection services)
    {
        services.AddSingleton<IContentDecoder, TextDecoder>();
        services.AddSingleton<IContentDecoder, MarkDownDecoder>();
        services.AddSingleton<IContentDecoder, HtmlDecoder>();
        services.AddSingleton<IContentDecoder, PdfDecoder>();
        services.AddSingleton<IContentDecoder, ImageDecoder>();
        services.AddSingleton<IContentDecoder, MsExcelDecoder>();
        services.AddSingleton<IContentDecoder, MsPowerPointDecoder>();
        services.AddSingleton<IContentDecoder, MsWordDecoder>();

        return services;
    }

    public static IServiceCollection AddDefaultWebScraper(
        this IServiceCollection services)
    {
        services.AddSingleton<IWebScraper, WebScraper>();
        return services;
    }

    public static IServiceCollection AddCustomWebScraper(
        this IServiceCollection services, IWebScraper webScraper)
    {
        services.AddSingleton<IWebScraper>(webScraper);
        return services;
    }

    public static IServiceCollection AddCustomWebScraper<T>(
        this IServiceCollection services) where T : class, IWebScraper
    {
        services.AddSingleton<IWebScraper, T>();
        return services;
    }
}
