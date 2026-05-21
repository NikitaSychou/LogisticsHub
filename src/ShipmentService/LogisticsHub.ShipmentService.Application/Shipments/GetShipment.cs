using LogisticsHub.ShipmentService.Application.Persistence;
using MediatR;

namespace LogisticsHub.ShipmentService.Application.Shipments;

public sealed class GetShipment : IRequestHandler<GetShipmentQuery, GetShipmentResult?>
{
    private readonly IShipmentDbContext _dbContext;

    public GetShipment(IShipmentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetShipmentResult?> Handle(
        GetShipmentQuery query,
        CancellationToken cancellationToken)
    {
        var shipment = await _dbContext.GetShipmentByIdAsync(query.Id, cancellationToken);

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
