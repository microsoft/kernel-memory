// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.SemanticMemory.Models;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.SemanticMemory;

/// <summary>
/// A document is a collection of one or multiple files, with additional
/// metadata such as tags and ownership.
/// </summary>
public class Document
{
    /// <summary>
    /// Regex to detect all special chars that might cause problems when used
    /// for file/folder names on local filesystems, cloud storage solutions, etc.
    /// </summary>
    private static readonly Regex s_replaceSymbolsRegex = new(@"[\s|\||\\|/|\0|'|\`|""|:|;|,|~|!|?|*|+|=|^|@|#|$|%|&]");

    /// <summary>
    /// Document ID, used also as Pipeline ID.
    /// </summary>
    public string Id
    {
        get { return this._id; }
        set
        {
            this._id = string.IsNullOrWhiteSpace(value)
                ? ValidateId(RandomId())
                : ValidateId(value);
        }
    }

    public FileCollection Files { get; } = new();

    public TagCollection Tags { get; } = new();

    public Document(string? id = null, TagCollection? tags = null, IEnumerable<string>? filePaths = null)
    {
        // Note: the value is polished by the property setter
        this.Id = id!;

        if (tags != null) { this.Tags = tags; }

        if (filePaths != null)
        {
            foreach (var filePath in filePaths)
            {
                this.Files.AddFile(filePath);
            }
        }
    }

    public Document AddTag(string name, string value)
    {
        this.Tags.Add(name, value);
        return this;
    }

    /// <summary>
    /// Add a file to the internal collection. If the file path is already in the list, the call is ignored.
    /// If a file with the same name (ignoring the path) already exists, the system generates a new unique
    /// file name, using the path string, anonymizing the path with a SHA algorithm.
    /// </summary>
    /// <param name="filePath">Full file path</param>
    public Document AddFile(string filePath)
    {
        this.Files.AddFile(filePath);
        return this;
    }

    /// <summary>
    /// Add a list of files to the internal collection. If any of file paths is already in the list, such file is ignored.
    /// If a file with the same name (ignoring the path) already exists, the system generates a new unique
    /// file name, using the path string, anonymizing the path with a SHA algorithm.
    /// </summary>
    /// <param name="filePaths">List of paths</param>
    public Document AddFiles(IEnumerable<string>? filePaths)
    {
        return this.AddFiles(filePaths?.ToArray());
    }

    /// <summary>
    /// Add a list of files to the internal collection. If any of file paths is already in the list, such file is ignored.
    /// If a file with the same name (ignoring the path) already exists, the system generates a new unique
    /// file name, using the path string, anonymizing the path with a SHA algorithm.
    /// </summary>
    /// <param name="filePaths">List of paths</param>
    public Document AddFiles(string[]? filePaths)
    {
        if (filePaths == null) { return this; }

        foreach (var filePath in filePaths)
        {
            this.Files.AddFile(filePath);
        }

        return this;
    }

    /// <summary>
    /// Add a stream content to the list of files to upload. If the file name already exists,
    /// a new name is generated, keeping both streams.
    /// </summary>
    /// <param name="fileName">Name of the stream</param>
    /// <param name="content">Stream content</param>
    public Document AddStream(string? fileName, Stream content)
    {
        if (content == null)
        {
            throw new SemanticMemoryException("The content stream is NULL");
        }

        this.Files.AddStream(fileName, content);
        return this;
    }

    /// <summary>
    /// Check for special chars to ensure the identifier is valid across multiple storage solutions.
    /// </summary>
    public static string ValidateId(string? id)
    {
        if (string.IsNullOrEmpty(id))
        {
            throw new ArgumentOutOfRangeException(nameof(id), "The document ID is empty");
        }

        if (s_replaceSymbolsRegex.IsMatch(id))
        {
            throw new ArgumentOutOfRangeException(nameof(id), "The document ID contains invalid symbols");
        }

        return id!;
    }

    /// <summary>
    /// Remove invalid chars from the input, replacing them with underscore. Chars are considered
    /// invalid if they have special meaning or are prohibited by some storage engines, e.g. '/' and '~'.
    /// </summary>
    /// <param name="value">Value to sanitize</param>
    /// <returns>Sanitized value</returns>
    public static string ReplaceInvalidChars(string? value)
    {
        if (value == null) { return string.Empty; }

        return s_replaceSymbolsRegex.Replace(value, "_");
    }

    #region private

    private string _id = string.Empty;

    private static string RandomId()
    {
        const string LocalDateFormat = "yyyyMMddhhmmssfffffff";
        return Guid.NewGuid().ToString("N") + DateTimeOffset.Now.ToString(LocalDateFormat, CultureInfo.InvariantCulture);
    }

    #endregion
}
