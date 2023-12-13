// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.KernelMemory.DataFormats.Image.AzureAIDocIntel;

public class AzureAIDocIntelConfig
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AuthTypes
    {
        Unknown = -1,
        AzureIdentity,
        APIKey,
    }

    public AuthTypes Auth { get; set; } = AuthTypes.Unknown;

    public string Endpoint { get; set; } = string.Empty;

    public string APIKey { get; set; } = string.Empty;

    /// <summary>
    /// Verify that the current state is valid.
    /// </summary>
    public void Validate()
    {
        if (this.Auth == AuthTypes.APIKey && string.IsNullOrWhiteSpace(this.APIKey))
        {
            throw new ArgumentOutOfRangeException(nameof(this.APIKey), "The API Key is empty");
        }

        if (string.IsNullOrWhiteSpace(this.Endpoint))
        {
            throw new ArgumentOutOfRangeException(nameof(this.Endpoint), "The endpoint value is empty");
        }

        if (!this.Endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentOutOfRangeException(nameof(this.Endpoint), "The endpoint value must start with https://");
        }
    }
}
