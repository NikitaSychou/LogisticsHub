using LogisticsHub.InventoryService.Application.Persistence;
using LogisticsHub.InventoryService.Domain.Entities;
using LogisticsHub.Results;
using MediatR;

namespace LogisticsHub.InventoryService.Application.InventoryItems;

public sealed class CreateInventoryItem : IRequestHandler<CreateInventoryItemCommand, Result<InventoryItemResult>>
{
    private readonly IInventoryDbContext _dbContext;

    public CreateInventoryItem(IInventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<InventoryItemResult>> Handle(
        CreateInventoryItemCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var existingItem = await _dbContext.GetItemBySkuAsync(command.Sku, cancellationToken);
        if (existingItem is not null)
        {
            return Result<InventoryItemResult>.Failure(InventoryItemErrors.AlreadyExists(command.Sku));
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

        return Result<InventoryItemResult>.Success(
            new InventoryItemResult(item.Sku, item.Name, stockBalance.OnHand - stockBalance.Reserved));
    }
}
