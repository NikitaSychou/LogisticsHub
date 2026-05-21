using LogisticsHub.ShipmentService.Application.Persistence;
using LogisticsHub.ShipmentService.Domain.Entities;
using LogisticsHub.ShipmentService.Domain.Enums;

namespace LogisticsHub.ShipmentService.Application.Shipments;

public sealed class MarkShipmentReservationFailed
{
    private readonly IShipmentDbContext _dbContext;

    public MarkShipmentReservationFailed(IShipmentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task ExecuteAsync(
        Guid eventId,
        Guid shipmentId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var alreadyProcessed = await _dbContext.HasShipmentInboxMessageAsync(eventId, cancellationToken);

        if (alreadyProcessed)
        {
            return;
        }

        var shipment = await _dbContext.GetShipmentForUpdateAsync(shipmentId, cancellationToken);

        if (shipment is null)
        {
            return;
        }

        var now = DateTime.UtcNow;

        if (shipment.Status == ShipmentStatus.ReservationRequested)
        {
            shipment.Status = ShipmentStatus.ReservationFailed;
            shipment.ReservationFailureReason = reason;
            shipment.UpdatedAt = now;
        }

        await _dbContext.AddShipmentInboxMessageAsync(
            new ShipmentInboxMessage
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                Type = "StockReservationFailedIntegrationEvent",
                ProcessedAtUtc = now,
                CreatedAtUtc = now
            },
            cancellationToken);

        await _dbContext.SaveChangesAsyncHandlingDuplicateInboxEventAsync(cancellationToken);
    }
}
