using LogisticsHub.InventoryService.Application.Persistence;
using LogisticsHub.Results;
using MediatR;

namespace LogisticsHub.InventoryService.Application.InventoryItems;

public sealed class GetInventoryItem : IRequestHandler<GetInventoryItemQuery, Result<InventoryItemResult>>
{
    private readonly IInventoryDbContext _dbContext;

    public GetInventoryItem(IInventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<InventoryItemResult>> Handle(
        GetInventoryItemQuery query,
        CancellationToken cancellationToken)
    {
        var item = await _dbContext.GetItemBySkuAsync(query.Sku, cancellationToken);

        if (item?.StockBalance is null)
        {
            return Result<InventoryItemResult>.Failure(InventoryItemErrors.NotFound(query.Sku));
        }

        var quantityAvailable = item.StockBalance.OnHand - item.StockBalance.Reserved;

        return Result<InventoryItemResult>.Success(new InventoryItemResult(item.Sku, item.Name, quantityAvailable));
    }
}
