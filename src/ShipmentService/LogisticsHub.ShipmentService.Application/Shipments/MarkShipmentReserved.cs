using LogisticsHub.ShipmentService.Application.Persistence;
using LogisticsHub.ShipmentService.Domain.Entities;
using LogisticsHub.ShipmentService.Domain.Enums;

namespace LogisticsHub.ShipmentService.Application.Shipments;

public sealed class MarkShipmentReserved
{
    private readonly IShipmentDbContext dbContext;

    public MarkShipmentReserved(IShipmentDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task ExecuteAsync(
        Guid eventId,
        Guid shipmentId,
        Guid reservationId,
        CancellationToken cancellationToken = default)
    {
        var alreadyProcessed = await dbContext.HasShipmentInboxMessageAsync(eventId, cancellationToken);

        if (alreadyProcessed)
        {
            return;
        }

        var shipment = await dbContext.GetShipmentForUpdateAsync(shipmentId, cancellationToken);

        if (shipment is null)
        {
            return;
        }

        var now = DateTime.UtcNow;

        if (shipment.Status == ShipmentStatus.ReservationRequested)
        {
            shipment.Status = ShipmentStatus.Reserved;
            shipment.ReservationId = reservationId;
            shipment.ReservationFailureReason = null;
            shipment.UpdatedAt = now;
        }

        await dbContext.AddShipmentInboxMessageAsync(
            new ShipmentInboxMessage
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                Type = "StockReservedIntegrationEvent",
                ProcessedAtUtc = now,
                CreatedAtUtc = now
            },
            cancellationToken);

        await dbContext.SaveChangesAsyncHandlingDuplicateInboxEventAsync(cancellationToken);
    }
}
