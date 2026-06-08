// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Aspire.Hosting;

namespace Microsoft.KernelMemory.Aspire;

public static class Dashboard
{
    /// <summary>
    /// Show Aspire dashboard URL before the logging start.
    /// </summary>
    public static IDistributedApplicationBuilder ShowDashboardUrl(this IDistributedApplicationBuilder builder, bool withStyle = false)
    {
        Console.WriteLine(withStyle
            ? $"\u001b[1mAspire dashboard URL: {GetUrl(builder)}\u001b[0m\n\n"
            : $"Aspire dashboard URL: {GetUrl(builder)}\n\n");
        return builder;
    }

    /// <summary>
    /// Wait 5 seconds and automatically open the browser (when using 'dotnet run' the browser doesn't open)
    /// </summary>
    public static IDistributedApplicationBuilder LaunchDashboard(this IDistributedApplicationBuilder builder, int delay = 5000)
    {
        Task.Run(async () =>
        {
            await Task.Delay(delay).ConfigureAwait(false);
            Process.Start(new ProcessStartInfo { FileName = GetUrl(builder), UseShellExecute = true });
        });

        return builder;
    }

    private static string GetUrl(IDistributedApplicationBuilder builder)
    {
        string token = builder.Configuration["AppHost:BrowserToken"] ?? string.Empty;
        string url = builder.Configuration["ASPNETCORE_URLS"]?.Split(";")[0] ?? throw new ArgumentException("ASPNETCORE_URLS is empty");
        return $"{url}/login?t={token}";
    }
}
