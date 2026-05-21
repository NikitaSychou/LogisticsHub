using LogisticsHub.ShipmentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsHub.ShipmentService.Infrastructure.Persistence.Configurations;

public sealed class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.ToTable("shipments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.ShipmentNumber)
            .HasColumnName("shipment_number");

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>();

        builder.Property(x => x.ReservationId)
            .HasColumnName("reservation_id");

        builder.Property(x => x.ReservationFailureReason)
            .HasColumnName("reservation_failure_reason");

        builder.Property(x => x.DestinationName)
            .HasColumnName("destination_name");

        builder.Property(x => x.DestinationAddress)
            .HasColumnName("destination_address");

        builder.Property(x => x.Comment)
            .HasColumnName("comment");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(x => x.DispatchedAt)
            .HasColumnName("dispatched_at");

        builder.Property(x => x.CancelledAt)
            .HasColumnName("cancelled_at");
    }
}
