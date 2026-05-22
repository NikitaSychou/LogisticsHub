using LogisticsHub.InventoryService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsHub.InventoryService.Infrastructure.Persistence.Configurations;

public sealed class InventoryOutboxMessageConfiguration : IEntityTypeConfiguration<InventoryOutboxMessage>
{
    public void Configure(EntityTypeBuilder<InventoryOutboxMessage> builder)
    {
        builder.ToTable("inventory_outbox_messages", "dbo");

        builder.HasKey(message => message.Id);

        builder.Property(message => message.Id)
            .HasColumnName("id");

        builder.Property(message => message.OccurredAtUtc)
            .HasColumnName("occurred_at_utc");

        builder.Property(message => message.Type)
            .HasColumnName("type")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(message => message.RoutingKey)
            .HasColumnName("routing_key")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(message => message.Payload)
            .HasColumnName("payload")
            .IsRequired();

        builder.Property(message => message.ProcessedAtUtc)
            .HasColumnName("processed_at_utc");

        builder.Property(message => message.LockedBy)
            .HasColumnName("locked_by")
            .HasMaxLength(256);

        builder.Property(message => message.LockedAtUtc)
            .HasColumnName("locked_at_utc");

        builder.Property(message => message.NextAttemptAtUtc)
            .HasColumnName("next_attempt_at_utc");

        builder.Property(message => message.FailedAtUtc)
            .HasColumnName("failed_at_utc");

        builder.Property(message => message.Error)
            .HasColumnName("error");

        builder.Property(message => message.RetryCount)
            .HasColumnName("retry_count");

        builder.Property(message => message.CreatedAtUtc)
            .HasColumnName("created_at_utc");
    }
}
