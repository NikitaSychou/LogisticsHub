using LogisticsHub.ShipmentService.Application.Persistence;
using LogisticsHub.ShipmentService.Domain.Entities;
using LogisticsHub.ShipmentService.Domain.Enums;
using MediatR;

namespace LogisticsHub.ShipmentService.Application.Shipments;

public sealed class MarkShipmentReserved : IRequestHandler<MarkShipmentReservedCommand>
{
    private readonly IShipmentDbContext _dbContext;

    public MarkShipmentReserved(IShipmentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(
        MarkShipmentReservedCommand command,
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
            shipment.Status = ShipmentStatus.Reserved;
            shipment.ReservationId = command.ReservationId;
            shipment.ReservationFailureReason = null;
            shipment.UpdatedAt = now;
        }

        await _dbContext.AddShipmentInboxMessageAsync(
            new ShipmentInboxMessage
            {
                Id = Guid.NewGuid(),
                EventId = command.EventId,
                Type = "StockReservedIntegrationEvent",
                ProcessedAtUtc = now,
                CreatedAtUtc = now
            },
            cancellationToken);

        await _dbContext.SaveChangesAsyncHandlingDuplicateInboxEventAsync(cancellationToken);
    }
}
