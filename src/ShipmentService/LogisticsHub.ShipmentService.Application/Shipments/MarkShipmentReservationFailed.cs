using LogisticsHub.ShipmentService.Application.Persistence;
using LogisticsHub.ShipmentService.Domain.Entities;
using LogisticsHub.ShipmentService.Domain.Enums;
using MediatR;

namespace LogisticsHub.ShipmentService.Application.Shipments;

public sealed class MarkShipmentReservationFailed : IRequestHandler<MarkShipmentReservationFailedCommand>
{
    private readonly IShipmentDbContext _dbContext;

    public MarkShipmentReservationFailed(IShipmentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(
        MarkShipmentReservationFailedCommand command,
        CancellationToken cancellationToken)
    {
        var alreadyProcessed = await _dbContext.HasShipmentInboxMessageAsync(command.EventId, cancellationToken);

        if (alreadyProcessed)
        {
            return;
        }

        var shipment = await _dbContext.GetShipmentForUpdateAsync(command.ShipmentId, cancellationToken);

        if (shipment is null)
        {
            return;
        }

        var now = DateTime.UtcNow;

        if (shipment.Status == ShipmentStatus.ReservationRequested)
        {
            shipment.Status = ShipmentStatus.ReservationFailed;
            shipment.ReservationFailureReason = command.Reason;
            shipment.UpdatedAt = now;
        }

        await _dbContext.AddShipmentInboxMessageAsync(
            new ShipmentInboxMessage
            {
                Id = Guid.NewGuid(),
                EventId = command.EventId,
                Type = "StockReservationFailedIntegrationEvent",
                ProcessedAtUtc = now,
                CreatedAtUtc = now
            },
            cancellationToken);

        await _dbContext.SaveChangesAsyncHandlingDuplicateInboxEventAsync(cancellationToken);
    }
}
