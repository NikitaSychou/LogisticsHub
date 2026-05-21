using LogisticsHub.ShipmentService.Application.Persistence;
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
        Guid shipmentId,
        Guid reservationId,
        CancellationToken cancellationToken = default)
    {
        var shipment = await dbContext.GetShipmentForUpdateAsync(shipmentId, cancellationToken);

        if (shipment is null)
        {
            return;
        }

        shipment.Status = ShipmentStatus.Reserved;
        shipment.ReservationId = reservationId;
        shipment.ReservationFailureReason = null;
        shipment.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
