// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.KernelMemory.Text;

public static class StringExtensions
{
    public static string NormalizeNewlines(this string text, bool trim = false)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        // We won't need more than the original length
        char[] buffer = new char[text.Length];
        int bufferPos = 0;

        // Skip leading whitespace if trimming
        int i = 0;
        if (trim)
        {
            while (i < text.Length && char.IsWhiteSpace(text[i])) { i++; }
        }

        // Tracks the last non-whitespace position written into buffer
        int lastNonWhitespacePos = -1;

        // Single pass: replace \r\n or \r with \n, record last non-whitespace
        for (; i < text.Length; i++)
        {
            char c = text[i];

            if (c == '\r')
            {
                // If \r\n then skip the \n
                if (i + 1 < text.Length && text[i + 1] == '\n') { i++; }

                // Write a single \n
                buffer[bufferPos] = '\n';
            }
            else
            {
                buffer[bufferPos] = c;
            }

            // If trimming, update lastNonWhitespacePos only when char isn't whitespace
            // If not trimming, always update because we keep everything
            if (!trim || !char.IsWhiteSpace(buffer[bufferPos]))
            {
                lastNonWhitespacePos = bufferPos;
            }

            bufferPos++;
        }

        // Cut off trailing whitespace if trimming
        // If every char was whitespace, lastNonWhitespacePos stays -1 and the result is an empty string
        int finalLength = (trim && lastNonWhitespacePos >= 0)
            ? lastNonWhitespacePos + 1
            : bufferPos;

        // Safety check if everything was trimmed away
        if (finalLength < 0) { finalLength = 0; }

        return new string(buffer, 0, finalLength);
    }
}
