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

        builder.Property(x => x.SenderCompanyId)
            .HasColumnName("sender_company_id")
            .IsRequired();

        builder.Property(x => x.SenderAddressId)
            .HasColumnName("sender_address_id")
            .IsRequired();

        builder.Property(x => x.ReceiverCompanyId)
            .HasColumnName("receiver_company_id")
            .IsRequired();

        builder.Property(x => x.ReceiverAddressId)
            .HasColumnName("receiver_address_id")
            .IsRequired();

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
