using LogisticsHub.InventoryService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsHub.InventoryService.Infrastructure.Persistence.Configurations;

public sealed class StockReservationItemConfiguration : IEntityTypeConfiguration<StockReservationItem>
{
    public void Configure(EntityTypeBuilder<StockReservationItem> builder)
    {
        builder.ToTable("stock_reservation_items", "dbo");

        builder.HasKey(stockReservationItem => new
        {
            stockReservationItem.ReservationId,
            stockReservationItem.ItemId
        });

        builder.Property(stockReservationItem => stockReservationItem.ReservationId)
            .HasColumnName("reservation_id");

        builder.Property(stockReservationItem => stockReservationItem.ItemId)
            .HasColumnName("item_id");

        builder.Property(stockReservationItem => stockReservationItem.Quantity)
            .HasColumnName("quantity");

        builder.HasOne(stockReservationItem => stockReservationItem.Reservation)
            .WithMany(stockReservation => stockReservation.Items)
            .HasForeignKey(stockReservationItem => stockReservationItem.ReservationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(stockReservationItem => stockReservationItem.Item)
            .WithMany()
            .HasForeignKey(stockReservationItem => stockReservationItem.ItemId);
    }
}
