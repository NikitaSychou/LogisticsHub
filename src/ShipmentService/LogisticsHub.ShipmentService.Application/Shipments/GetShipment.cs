using LogisticsHub.ShipmentService.Application.Persistence;
using LogisticsHub.Results;
using MediatR;

namespace LogisticsHub.ShipmentService.Application.Shipments;

public sealed class GetShipment : IRequestHandler<GetShipmentQuery, Result<GetShipmentResult>>
{
    private readonly IShipmentDbContext _dbContext;

    public GetShipment(IShipmentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<GetShipmentResult>> Handle(
        GetShipmentQuery query,
        CancellationToken cancellationToken)
    {
        var shipment = await _dbContext.GetShipmentByIdAsync(query.Id, cancellationToken);

        if (shipment is null)
        {
            return Result<GetShipmentResult>.Failure(ShipmentErrors.NotFound(query.Id));
        }

        return Result<GetShipmentResult>.Success(
            new GetShipmentResult(
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
                    .ToArray()));
    }
}
