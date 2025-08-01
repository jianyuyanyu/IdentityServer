// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.Bff.SessionManagement.SessionStore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Duende.Bff.EntityFramework;

/// <summary>
/// Extension methods to define the database schema for the session data store.
/// </summary>

public static class ModelBuilderExtensions
{
    private static EntityTypeBuilder<TEntity> ToTable<TEntity>(this EntityTypeBuilder<TEntity> entityTypeBuilder, TableConfiguration configuration)
        where TEntity : class => string.IsNullOrWhiteSpace(configuration.Schema) ?
            entityTypeBuilder.ToTable(configuration.Name) :
            entityTypeBuilder.ToTable(configuration.Name, configuration.Schema);

    /// <summary>
    /// Configures the persisted grant context.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <param name="storeOptions">The store options.</param>
    public static void ConfigureSessionContext(this ModelBuilder modelBuilder, SessionStoreOptions storeOptions)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        ArgumentNullException.ThrowIfNull(storeOptions);
        if (!string.IsNullOrWhiteSpace(storeOptions.DefaultSchema))
        {
            modelBuilder.HasDefaultSchema(storeOptions.DefaultSchema);
        }

        modelBuilder.Entity<UserSessionEntity>(entity =>
        {
            entity.ToTable(storeOptions.UserSessions);

            entity.HasKey(x => x.Id);

            entity.Property(x => x.PartitionKey).HasConversion<PartitionKeyConverter>().HasMaxLength(200);
            entity.Property(x => x.Key).HasConversion<UserKeyConverter>().IsRequired().HasMaxLength(200);
            entity.Property(x => x.SubjectId).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Ticket).IsRequired();

            entity.HasIndex(x => new { ApplicationName = x.PartitionKey, x.Key }).IsUnique();
            entity.HasIndex(x => new { ApplicationName = x.PartitionKey, x.SubjectId, x.SessionId }).IsUnique();
            entity.HasIndex(x => new { ApplicationName = x.PartitionKey, x.SessionId }).IsUnique();
            entity.HasIndex(x => x.Expires);
        });
    }

    public class UserKeyConverter() : ValueConverter<UserKey, string>(
        key => key.ToString(),
        value => UserKey.Parse(value));

    public class PartitionKeyConverter() : ValueConverter<PartitionKey, string>(
        key => key.ToString(),
        value => PartitionKey.Parse(value));
}
