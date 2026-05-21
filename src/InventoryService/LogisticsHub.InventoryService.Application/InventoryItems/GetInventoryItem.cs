using LogisticsHub.InventoryService.Application.Persistence;
using MediatR;

namespace LogisticsHub.InventoryService.Application.InventoryItems;

public sealed class GetInventoryItem : IRequestHandler<GetInventoryItemQuery, InventoryItemResult?>
{
    private readonly IInventoryDbContext _dbContext;

    public GetInventoryItem(IInventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<InventoryItemResult?> Handle(
        GetInventoryItemQuery query,
        CancellationToken cancellationToken)
    {
        var item = await _dbContext.GetItemBySkuAsync(query.Sku, cancellationToken);

        if (item?.StockBalance is null)
        {
            return null;
        }

        var quantityAvailable = item.StockBalance.OnHand - item.StockBalance.Reserved;

        return new InventoryItemResult(item.Sku, item.Name, quantityAvailable);
    }
}
