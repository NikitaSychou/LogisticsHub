using LogisticsHub.InventoryService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsHub.InventoryService.Infrastructure.Persistence.Configurations;

public sealed class StockReservationConfiguration : IEntityTypeConfiguration<StockReservation>
{
    public void Configure(EntityTypeBuilder<StockReservation> builder)
    {
        builder.ToTable("stock_reservations", "dbo");

        builder.HasKey(stockReservation => stockReservation.Id);

        builder.Property(stockReservation => stockReservation.Id)
            .HasColumnName("id");

        builder.Property(stockReservation => stockReservation.ShipmentId)
            .HasColumnName("shipment_id");

        builder.Property(stockReservation => stockReservation.Status)
            .HasColumnName("status")
            .HasMaxLength(32)
            .HasConversion<string>();

        builder.Property(stockReservation => stockReservation.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(stockReservation => stockReservation.UpdatedAt)
            .HasColumnName("updated_at");
    }
}
