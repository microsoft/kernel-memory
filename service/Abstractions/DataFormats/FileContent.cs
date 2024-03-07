// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.KernelMemory.DataFormats;

public class FileContent
{
    [JsonPropertyOrder(0)]
    [JsonPropertyName("sections")]
    public List<FileSection> Sections { get; set; } = new();
}
