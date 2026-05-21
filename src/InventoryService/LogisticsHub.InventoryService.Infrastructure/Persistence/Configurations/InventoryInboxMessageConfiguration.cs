using LogisticsHub.InventoryService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsHub.InventoryService.Infrastructure.Persistence.Configurations;

public sealed class InventoryInboxMessageConfiguration : IEntityTypeConfiguration<InventoryInboxMessage>
{
    public void Configure(EntityTypeBuilder<InventoryInboxMessage> builder)
    {
        builder.ToTable("inventory_inbox_messages", "dbo");

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
            .HasColumnName("processed_at_utc");

        builder.Property(message => message.CreatedAtUtc)
            .HasColumnName("created_at_utc");

        builder.HasIndex(message => message.EventId)
            .IsUnique()
            .HasDatabaseName("IX_inventory_inbox_messages_event_id");
    }
}
