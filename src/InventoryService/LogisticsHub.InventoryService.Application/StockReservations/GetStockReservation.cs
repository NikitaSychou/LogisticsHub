using LogisticsHub.InventoryService.Application.Persistence;
using LogisticsHub.Results;
using MediatR;

namespace LogisticsHub.InventoryService.Application.StockReservations;

public sealed class GetStockReservation : IRequestHandler<GetStockReservationQuery, Result<StockReservationResult>>
{
    private readonly IInventoryDbContext _dbContext;

    public GetStockReservation(IInventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<StockReservationResult>> Handle(
        GetStockReservationQuery query,
        CancellationToken cancellationToken)
    {
        var stockReservation = await _dbContext.GetStockReservationByIdAsync(query.ReservationId, cancellationToken);

        if (stockReservation is null)
        {
            return Result<StockReservationResult>.Failure(StockReservationErrors.NotFound(query.ReservationId));
        }

        var items = stockReservation.Items
            .Where(item => item.Item is not null)
            .Select(item => new StockReservationItemResult(item.Item!.Sku, item.Quantity))
            .ToArray();

        return Result<StockReservationResult>.Success(
            new StockReservationResult(
                stockReservation.Id,
                stockReservation.ShipmentId,
                stockReservation.Status,
                items));
    }
}
