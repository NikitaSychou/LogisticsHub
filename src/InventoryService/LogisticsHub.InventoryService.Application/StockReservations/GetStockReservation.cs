using LogisticsHub.InventoryService.Application.Persistence;
using MediatR;

namespace LogisticsHub.InventoryService.Application.StockReservations;

public sealed class GetStockReservation : IRequestHandler<GetStockReservationQuery, StockReservationResult?>
{
    private readonly IInventoryDbContext _dbContext;

    public GetStockReservation(IInventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<StockReservationResult?> Handle(
        GetStockReservationQuery query,
        CancellationToken cancellationToken)
    {
        var stockReservation = await _dbContext.GetStockReservationByIdAsync(query.ReservationId, cancellationToken);

        if (stockReservation is null)
        {
            return null;
        }

        var items = stockReservation.Items
            .Where(item => item.Item is not null)
            .Select(item => new StockReservationItemResult(item.Item!.Sku, item.Quantity))
            .ToArray();

        return new StockReservationResult(
            stockReservation.Id,
            stockReservation.ShipmentId,
            stockReservation.Status,
            items);
    }
}
