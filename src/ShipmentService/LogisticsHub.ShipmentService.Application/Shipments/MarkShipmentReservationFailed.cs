using LogisticsHub.ShipmentService.Application.Persistence;
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
        Guid shipmentId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var shipment = await dbContext.GetShipmentForUpdateAsync(shipmentId, cancellationToken);

        if (shipment is null)
        {
            return;
        }

        shipment.Status = ShipmentStatus.ReservationFailed;
        shipment.ReservationFailureReason = reason;
        shipment.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
