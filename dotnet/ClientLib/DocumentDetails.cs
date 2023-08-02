// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Globalization;

namespace Microsoft.SemanticMemory.Client;

public class DocumentDetails
{
    public string DocumentId
    {
        get { return this._documentId; }
        set { this._documentId = string.IsNullOrWhiteSpace(value) ? RandomId() : value; }
    }

    public string UserId
    {
        get { return this._userId; }
        set { this._userId = string.IsNullOrWhiteSpace(value) ? Constants.DefaultDocumentOwnerUserId : value; }
    }

    public TagCollection Tags { get; set; } = new();

    public DocumentDetails(
        string userId = Constants.DefaultDocumentOwnerUserId,
        string documentId = "",
        TagCollection? tags = null)
    {
        this.DocumentId = string.IsNullOrEmpty(documentId) ? RandomId() : documentId;
        this.UserId = userId;
        if (tags != null) { this.Tags = tags; }
    }

    public DocumentDetails AddTag(string name, string value)
    {
        this.Tags.Add(name, value);
        return this;
    }

    #region private

    private string _userId = string.Empty;
    private string _documentId = string.Empty;

    private static string RandomId()
    {
        const string LocalDateFormat = "yyyyMMddhhmmssfffffff";
        return Guid.NewGuid().ToString("N") + DateTimeOffset.Now.ToString(LocalDateFormat, CultureInfo.InvariantCulture);
    }

    #endregion
}
