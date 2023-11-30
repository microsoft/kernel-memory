// Copyright (c) Microsoft. All rights reserved.

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
}
