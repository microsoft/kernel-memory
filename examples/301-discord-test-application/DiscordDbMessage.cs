// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel.DataAnnotations;
using Microsoft.KernelMemory.Sources.DiscordBot;

namespace Microsoft.Discord.TestApplication;

public class DiscordDbMessage : DiscordMessage
{
    [Key]
    public string Id
    {
        get
        {
            return this.MessageId;
        }
        set
        {
            this.MessageId = value;
        }
    }
}
