// Copyright (c) Microsoft. All rights reserved.
using System.Globalization;
using KernelMemory.Core.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace KernelMemory.Core.Storage;

/// <summary>
/// Database context for content storage.
/// Manages Content and Operations tables with proper SQLite configuration.
/// </summary>
public class ContentStorageDbContext : DbContext
{
    public DbSet<ContentRecord> Content { get; set; } = null!;
    public DbSet<OperationRecord> Operations { get; set; } = null!;

    public ContentStorageDbContext(DbContextOptions<ContentStorageDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Content table
        modelBuilder.Entity<ContentRecord>(entity =>
        {
            // Hardcoded table name as per specification
            entity.ToTable("km_content");

            // Primary key
            entity.HasKey(e => e.Id);

            // Required fields
            entity.Property(e => e.Id)
                .IsRequired()
                .HasMaxLength(32); // Cuid2 is typically 25-32 characters

            entity.Property(e => e.Content)
                .IsRequired();

            entity.Property(e => e.MimeType)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.ByteSize)
                .IsRequired();

            entity.Property(e => e.Ready)
                .IsRequired();

            // DateTimeOffset stored as ISO 8601 string in SQLite
            entity.Property(e => e.ContentCreatedAt)
                .IsRequired()
                .HasConversion(
                    v => v.ToString("O"),
                    v => DateTimeOffset.Parse(v, CultureInfo.InvariantCulture));

            entity.Property(e => e.RecordCreatedAt)
                .IsRequired()
                .HasConversion(
                    v => v.ToString("O"),
                    v => DateTimeOffset.Parse(v, CultureInfo.InvariantCulture));

            entity.Property(e => e.RecordUpdatedAt)
                .IsRequired()
                .HasConversion(
                    v => v.ToString("O"),
                    v => DateTimeOffset.Parse(v, CultureInfo.InvariantCulture));

            // Optional fields
            entity.Property(e => e.Title)
                .IsRequired()
                .HasDefaultValue(string.Empty);

            entity.Property(e => e.Description)
                .IsRequired()
                .HasDefaultValue(string.Empty);

            // JSON fields - store as TEXT in SQLite
            entity.Property(e => e.TagsJson)
                .IsRequired()
                .HasColumnType("TEXT")
                .HasDefaultValue("[]");

            entity.Property(e => e.MetadataJson)
                .IsRequired()
                .HasColumnType("TEXT")
                .HasDefaultValue("{}");

            // Ignore computed properties (not stored in database)
            entity.Ignore(e => e.Tags);
            entity.Ignore(e => e.Metadata);

            // Indexes
            entity.HasIndex(e => e.Ready)
                .HasDatabaseName("IX_km_content_Ready");
        });

        // Configure Operations table
        modelBuilder.Entity<OperationRecord>(entity =>
        {
            // Hardcoded table name as per specification
            entity.ToTable("km_operations");

            // Primary key
            entity.HasKey(e => e.Id);

            // Required fields
            entity.Property(e => e.Id)
                .IsRequired()
                .HasMaxLength(32);

            entity.Property(e => e.Complete)
                .IsRequired();

            entity.Property(e => e.Cancelled)
                .IsRequired();

            entity.Property(e => e.ContentId)
                .IsRequired()
                .HasMaxLength(32);

            // DateTimeOffset stored as ISO 8601 string in SQLite
            entity.Property(e => e.Timestamp)
                .IsRequired()
                .HasConversion(
                    v => v.ToString("O"),
                    v => DateTimeOffset.Parse(v, CultureInfo.InvariantCulture));

            entity.Property(e => e.LastFailureReason)
                .IsRequired()
                .HasDefaultValue(string.Empty);

            // Nullable DateTimeOffset for locking
            entity.Property(e => e.LastAttemptTimestamp)
                .HasConversion(
                    v => v.HasValue ? v.Value.ToString("O") : null,
                    v => v == null ? (DateTimeOffset?)null : DateTimeOffset.Parse(v, CultureInfo.InvariantCulture));

            // JSON fields - store as TEXT in SQLite
            entity.Property(e => e.PlannedStepsJson)
                .IsRequired()
                .HasColumnType("TEXT")
                .HasDefaultValue("[]");

            entity.Property(e => e.CompletedStepsJson)
                .IsRequired()
                .HasColumnType("TEXT")
                .HasDefaultValue("[]");

            entity.Property(e => e.RemainingStepsJson)
                .IsRequired()
                .HasColumnType("TEXT")
                .HasDefaultValue("[]");

            entity.Property(e => e.PayloadJson)
                .IsRequired()
                .HasColumnType("TEXT")
                .HasDefaultValue("{}");

            // Ignore computed properties (not stored in database)
            entity.Ignore(e => e.PlannedSteps);
            entity.Ignore(e => e.CompletedSteps);
            entity.Ignore(e => e.RemainingSteps);
            entity.Ignore(e => e.Payload);

            // Indexes as per specification
            entity.HasIndex(e => new { e.ContentId, e.Timestamp })
                .HasDatabaseName("IX_km_operations_ContentId_Timestamp");

            entity.HasIndex(e => new { e.Complete, e.Timestamp })
                .HasDatabaseName("IX_km_operations_Complete_Timestamp");

            entity.HasIndex(e => e.Timestamp)
                .HasDatabaseName("IX_km_operations_Timestamp");
        });
    }
}
