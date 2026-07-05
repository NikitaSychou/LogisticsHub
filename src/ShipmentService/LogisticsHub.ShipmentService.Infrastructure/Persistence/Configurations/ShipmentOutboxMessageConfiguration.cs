using LogisticsHub.ShipmentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsHub.ShipmentService.Infrastructure.Persistence.Configurations;

public sealed class ShipmentOutboxMessageConfiguration : IEntityTypeConfiguration<ShipmentOutboxMessage>
{
    public void Configure(EntityTypeBuilder<ShipmentOutboxMessage> builder)
    {
        builder.ToTable("shipment_outbox_messages", "dbo");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.OccurredAtUtc)
            .HasColumnName("occurred_at_utc")
            .HasPrecision(7);

        builder.Property(x => x.Type)
            .HasColumnName("type")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(x => x.RoutingKey)
            .HasColumnName("routing_key")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.Payload)
            .HasColumnName("payload")
            .IsRequired();

        builder.Property(x => x.ProcessedAtUtc)
            .HasColumnName("processed_at_utc")
            .HasPrecision(7);

        builder.Property(x => x.LockedBy)
            .HasColumnName("locked_by")
            .HasMaxLength(256);

        builder.Property(x => x.LockedAtUtc)
            .HasColumnName("locked_at_utc")
            .HasPrecision(7);

        builder.Property(x => x.NextAttemptAtUtc)
            .HasColumnName("next_attempt_at_utc")
            .HasPrecision(7);

        builder.Property(x => x.FailedAtUtc)
            .HasColumnName("failed_at_utc")
            .HasPrecision(7);

        builder.Property(x => x.Error)
            .HasColumnName("error");

        builder.Property(x => x.RetryCount)
            .HasColumnName("retry_count");

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasPrecision(7);
    }
}
