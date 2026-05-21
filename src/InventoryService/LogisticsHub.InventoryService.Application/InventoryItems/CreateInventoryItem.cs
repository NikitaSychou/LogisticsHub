using LogisticsHub.InventoryService.Application.Persistence;
using LogisticsHub.InventoryService.Domain.Entities;
using MediatR;

namespace LogisticsHub.InventoryService.Application.InventoryItems;

public sealed class CreateInventoryItem : IRequestHandler<CreateInventoryItemCommand, InventoryItemResult?>
{
    private readonly IInventoryDbContext _dbContext;

    public CreateInventoryItem(IInventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<InventoryItemResult?> Handle(
        CreateInventoryItemCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var existingItem = await _dbContext.GetItemBySkuAsync(command.Sku, cancellationToken);
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

        await _dbContext.AddItemAsync(item, cancellationToken);
        await _dbContext.AddStockBalanceAsync(stockBalance, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new InventoryItemResult(item.Sku, item.Name, stockBalance.OnHand - stockBalance.Reserved);
    }
}
