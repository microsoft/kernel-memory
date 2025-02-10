// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using Microsoft.KernelMemory.Text;

namespace Microsoft.KernelMemory.HTTP;

// See https://developer.mozilla.org/docs/Web/API/Server-sent_events/Using_server-sent_events
public static class SSE
{
    public const string DataPrefix = "data: ";
    public const string LastToken = "[DONE]";
    public const string DoneMessage = $"{DataPrefix}{LastToken}";

    public async static IAsyncEnumerable<T> ParseStreamAsync<T>(
        Stream stream, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8);
        StringBuilder buffer = new();

        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line)) // \n\n detected => Message delimiter
            {
                if (buffer.Length == 0) { continue; }

                string message = buffer.ToString();
                buffer.Clear();
                if (message.Trim() == DoneMessage) { yield break; }

                var value = ParseMessage<T>(message);
                if (value != null) { yield return value; }
            }
            else
            {
                buffer.AppendLineNix(line);
            }
        }

        // Process any remaining text as the last message
        if (buffer.Length > 0)
        {
            string message = buffer.ToString();
            if (message.Trim() == DoneMessage) { yield break; }

            var value = ParseMessage<T>(message);
            if (value != null) { yield return value; }
        }
    }

    public static T? ParseMessage<T>(string? message)
    {
        if (string.IsNullOrWhiteSpace(message)) { return default; }

        string json = string.Join("",
            message.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Where(line => line.StartsWith(DataPrefix, StringComparison.OrdinalIgnoreCase))
                .Select(line => line[DataPrefix.Length..]));

        return JsonSerializer.Deserialize<T>(json);
    }
}
