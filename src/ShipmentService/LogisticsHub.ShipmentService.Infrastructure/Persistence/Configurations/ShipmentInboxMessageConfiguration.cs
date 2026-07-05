using LogisticsHub.ShipmentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsHub.ShipmentService.Infrastructure.Persistence.Configurations;

public sealed class ShipmentInboxMessageConfiguration : IEntityTypeConfiguration<ShipmentInboxMessage>
{
    public void Configure(EntityTypeBuilder<ShipmentInboxMessage> builder)
    {
        builder.ToTable("shipment_inbox_messages", "dbo");

        builder.HasKey(message => message.Id);

        builder.Property(message => message.Id)
            .HasColumnName("id");

        builder.Property(message => message.EventId)
            .HasColumnName("event_id");

        builder.Property(message => message.Type)
            .HasColumnName("type")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(message => message.ProcessedAtUtc)
            .HasColumnName("processed_at_utc")
            .HasPrecision(7);

        builder.Property(message => message.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasPrecision(7);

        builder.HasIndex(message => message.EventId)
            .IsUnique()
            .HasDatabaseName("IX_shipment_inbox_messages_event_id");
    }
}
