using LogisticsHub.ShipmentService.Application.Persistence;
using LogisticsHub.ShipmentService.Domain.Entities;
using LogisticsHub.ShipmentService.Domain.Enums;

namespace LogisticsHub.ShipmentService.Application.Shipments;

public sealed class MarkShipmentReservationFailed
{
    private readonly IShipmentDbContext dbContext;

    public MarkShipmentReservationFailed(IShipmentDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task ExecuteAsync(
        Guid eventId,
        Guid shipmentId,
        string reason,
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

        shipment.Status = ShipmentStatus.ReservationFailed;
        shipment.ReservationFailureReason = reason;
        shipment.UpdatedAt = now;

        await dbContext.AddShipmentInboxMessageAsync(
            new ShipmentInboxMessage
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                Type = "StockReservationFailedIntegrationEvent",
                ProcessedAtUtc = now,
                CreatedAtUtc = now
            },
            cancellationToken);

        await dbContext.SaveChangesAsyncHandlingDuplicateInboxEventAsync(cancellationToken);
    }
}
