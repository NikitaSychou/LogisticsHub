using LogisticsHub.ShipmentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsHub.ShipmentService.Infrastructure.Persistence.Configurations;

public sealed class ShipmentItemConfiguration : IEntityTypeConfiguration<ShipmentItem>
{
    public void Configure(EntityTypeBuilder<ShipmentItem> builder)
    {
        builder.ToTable("shipment_items");

        builder.HasKey(x => new { x.ShipmentId, x.Sku });

        builder.Property(x => x.ShipmentId)
            .HasColumnName("shipment_id");

        builder.Property(x => x.Sku)
            .HasColumnName("sku")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.Quantity)
            .HasColumnName("quantity");

        builder.HasOne<Shipment>()
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.ShipmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
