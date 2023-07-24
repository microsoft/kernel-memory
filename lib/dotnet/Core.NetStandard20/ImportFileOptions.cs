// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.SemanticKernel.SemanticMemory.Core20;

public class ImportFileOptions
{
    public string UserId { get; set; } = string.Empty;
    public List<string> VaultIds { get; set; } = new();
    public string RequestId { get; set; } = string.Empty;

    public ImportFileOptions()
    {
    }

    public ImportFileOptions(string userId, string vaultId)
        : this(userId, vaultId, string.Empty)
    {
    }

    public ImportFileOptions(string userId, string vaultId, string requestId)
    {
        this.UserId = userId;
        this.VaultIds.Add(vaultId);
        this.RequestId = requestId;
    }

    public ImportFileOptions(string userId, List<string> vaultIds, string requestId)
    {
        this.UserId = userId;
        this.VaultIds = vaultIds;
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

        if (this.VaultIds.Count < 1)
        {
            throw new ArgumentNullException(nameof(this.VaultIds), "The list of vaults is empty");
        }
    }
}
