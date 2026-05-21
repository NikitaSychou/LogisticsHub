using LogisticsHub.ShipmentService.Application.Persistence;
using LogisticsHub.ShipmentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogisticsHub.ShipmentService.Infrastructure.Persistence;

public class ShipmentDbContext : DbContext, IShipmentDbContext
{
    public ShipmentDbContext(DbContextOptions<ShipmentDbContext> options) : base(options)
    {
    }

    public DbSet<Shipment> Shipments { get; set; }
    public DbSet<ShipmentItem> ShipmentItems { get; set; }
    public DbSet<ShipmentOutboxMessage> ShipmentOutboxMessages { get; set; }

    public async Task<Shipment?> GetShipmentByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Shipments
            .AsNoTracking()
            .Include(shipment => shipment.Items)
            .SingleOrDefaultAsync(shipment => shipment.Id == id, cancellationToken);
    }

    public async Task<Shipment?> GetShipmentForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Shipments
            .SingleOrDefaultAsync(shipment => shipment.Id == id, cancellationToken);
    }

    public async Task AddShipmentAsync(Shipment shipment, CancellationToken cancellationToken = default)
    {
        await Shipments.AddAsync(shipment, cancellationToken);
    }

    public async Task AddShipmentItemAsync(ShipmentItem shipmentItem, CancellationToken cancellationToken = default)
    {
        await ShipmentItems.AddAsync(shipmentItem, cancellationToken);
    }

    public async Task AddShipmentOutboxMessageAsync(
        ShipmentOutboxMessage outboxMessage,
        CancellationToken cancellationToken = default)
    {
        await ShipmentOutboxMessages.AddAsync(outboxMessage, cancellationToken);
    }

    public async Task<IReadOnlyList<ShipmentOutboxMessage>> GetUnprocessedShipmentOutboxMessagesAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        return await ShipmentOutboxMessages
            .Where(message => message.ProcessedAtUtc == null)
            .OrderBy(message => message.OccurredAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Shipment>(builder =>
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
        });

        modelBuilder.Entity<ShipmentItem>(builder =>
        {
            builder.ToTable("shipment_items");

            builder.HasKey(x => new { x.ShipmentId, x.Sku });

            builder.Property(x => x.ShipmentId)
                .HasColumnName("shipment_id");

            builder.Property(x => x.Sku)
                .HasColumnName("sku");

            builder.Property(x => x.Quantity)
                .HasColumnName("quantity");

            builder.HasOne<Shipment>()
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.ShipmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ShipmentOutboxMessage>(builder =>
        {
            builder.ToTable("shipment_outbox_messages", "dbo");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("id");

            builder.Property(x => x.OccurredAtUtc)
                .HasColumnName("occurred_at_utc");

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
                .HasColumnName("processed_at_utc");

            builder.Property(x => x.Error)
                .HasColumnName("error");

            builder.Property(x => x.RetryCount)
                .HasColumnName("retry_count");

            builder.Property(x => x.CreatedAtUtc)
                .HasColumnName("created_at_utc");
        });
    }
}
