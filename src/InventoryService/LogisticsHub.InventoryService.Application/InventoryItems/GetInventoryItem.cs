using LogisticsHub.InventoryService.Application.Persistence;

namespace LogisticsHub.InventoryService.Application.InventoryItems;

public sealed class GetInventoryItem
{
    private readonly IInventoryDbContext dbContext;

    public GetInventoryItem(IInventoryDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<InventoryItemResult?> ExecuteAsync(
        string sku,
        CancellationToken cancellationToken = default)
    {
        var item = await dbContext.GetItemBySkuAsync(sku, cancellationToken);

        if (item?.StockBalance is null)
        {
            return null;
        }

        var quantityAvailable = item.StockBalance.OnHand - item.StockBalance.Reserved;

        return new InventoryItemResult(item.Sku, item.Name, quantityAvailable);
    }
}
