using LogisticsHub.InventoryService.Application.Persistence;
using LogisticsHub.InventoryService.Domain.Entities;

namespace LogisticsHub.InventoryService.Application.InventoryItems;

public sealed class CreateInventoryItem
{
    private readonly IInventoryDbContext dbContext;

    public CreateInventoryItem(IInventoryDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<InventoryItemResult?> ExecuteAsync(
        CreateInventoryItemCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var existingItem = await dbContext.GetItemBySkuAsync(command.Sku, cancellationToken);
        if (existingItem is not null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var item = new Item
        {
            Id = Guid.NewGuid(),
            Sku = command.Sku,
            Name = command.Name,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        var stockBalance = new StockBalance
        {
            ItemId = item.Id,
            OnHand = command.QuantityAvailable,
            Reserved = 0,
            UpdatedAt = now
        };

        await dbContext.AddItemAsync(item, cancellationToken);
        await dbContext.AddStockBalanceAsync(stockBalance, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new InventoryItemResult(item.Sku, item.Name, stockBalance.OnHand - stockBalance.Reserved);
    }
}
