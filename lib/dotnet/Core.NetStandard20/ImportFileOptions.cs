// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.SemanticMemory.Core20;

public class ImportFileOptions
{
    public string UserId { get; set; } = string.Empty;
    public List<string> CollectionIds { get; set; } = new();
    public string RequestId { get; set; } = string.Empty;

    public ImportFileOptions()
    {
    }

    public ImportFileOptions(string userId, string collectionId)
        : this(userId, collectionId, string.Empty)
    {
    }

    public ImportFileOptions(string userId, string collectionId, string requestId)
    {
        this.UserId = userId;
        this.CollectionIds.Add(collectionId);
        this.RequestId = requestId;
    }

    public ImportFileOptions(string userId, List<string> collectionIds, string requestId)
    {
        this.UserId = userId;
        this.CollectionIds = collectionIds;
        this.RequestId = requestId;
    }

    public void Sanitize()
    {
        if (string.IsNullOrEmpty(this.RequestId))
        {
            // note: the ID doesn't include the full date, to avoid "personal" details
            this.RequestId = Guid.NewGuid().ToString("D") + "-" + DateTimeOffset.UtcNow.ToString("ss.fffffff", CultureInfo.InvariantCulture);
        }
    }

    public void Validate()
    {
        if (string.IsNullOrEmpty(this.UserId))
        {
            throw new ArgumentNullException(nameof(this.UserId), "User ID is empty");
        }

        if (this.CollectionIds.Count < 1)
        {
            throw new ArgumentNullException(nameof(this.CollectionIds), "The list of collections is empty");
        }
    }
}
