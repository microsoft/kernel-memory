// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// ReSharper disable CommentTypo

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
            // this.RequestId = Guid.NewGuid().ToString("D") + "-" + DateTimeOffset.UtcNow.ToString("yyyyMMdd.HHmmss.fffffff");
            this.RequestId = Guid.NewGuid().ToString("D") + "-" + DateTimeOffset.UtcNow.ToString("ss.fffffff");
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

public interface ISemanticMemoryClient
{
    public Task ImportFileAsync(string file, ImportFileOptions options);
    public Task ImportFilesAsync(string[] files, ImportFileOptions options);
}
