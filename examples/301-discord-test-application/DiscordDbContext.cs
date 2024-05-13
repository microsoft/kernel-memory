// Copyright (c) Microsoft. All rights reserved.

using Microsoft.EntityFrameworkCore;

namespace Microsoft.Discord.TestApplication;

public class DiscordDbContext : DbContext
{
    public DbContextOptions<DiscordDbContext> Options { get; }

    public DbSet<DiscordDbMessage> Messages { get; set; }

    public DiscordDbContext(DbContextOptions<DiscordDbContext> options) : base(options)
    {
        this.Options = options;
    }
}
