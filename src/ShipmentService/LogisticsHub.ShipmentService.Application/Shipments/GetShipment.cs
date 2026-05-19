using LogisticsHub.ShipmentService.Application.Persistence;

namespace LogisticsHub.ShipmentService.Application.Shipments;

public sealed class GetShipment
{
    private readonly IShipmentDbContext dbContext;

    public GetShipment(IShipmentDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<GetShipmentResult?> ExecuteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var shipment = await dbContext.GetShipmentByIdAsync(id, cancellationToken);

        if (shipment is null)
        {
            return null;
        }

        return new GetShipmentResult(
            shipment.Id,
            shipment.ShipmentNumber,
            shipment.Status,
            shipment.ReservationId,
            shipment.ReservationFailureReason,
            shipment.DestinationName,
            shipment.DestinationAddress,
            shipment.Comment,
            shipment.CreatedAt,
            shipment.UpdatedAt,
            shipment.DispatchedAt,
            shipment.CancelledAt,
            shipment.Items
                .Select(item => new GetShipmentItemResult(item.Sku, item.Quantity))
                .ToArray());
    }
}
