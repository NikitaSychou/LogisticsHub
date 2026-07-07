using LogisticsHub.InventoryService.Application.Persistence;
using LogisticsHub.Results;
using MediatR;

namespace LogisticsHub.InventoryService.Application.InventoryItems;

public sealed class IncreaseInventoryItemStock : IRequestHandler<IncreaseInventoryItemStockCommand, Result<InventoryItemResult>>
{
    private readonly IInventoryDbContext _dbContext;

    public IncreaseInventoryItemStock(IInventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<InventoryItemResult>> Handle(
        IncreaseInventoryItemStockCommand command,
        CancellationToken cancellationToken)
    {
        var item = await _dbContext.GetItemForUpdateBySkuAsync(command.Sku, cancellationToken);
        if (item?.StockBalance is null)
        {
            return Result<InventoryItemResult>.Failure(InventoryItemErrors.NotFound(command.Sku));
        }

        var now = DateTime.UtcNow;
        item.StockBalance.OnHand += command.Quantity;
        item.StockBalance.UpdatedAt = now;
        item.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<InventoryItemResult>.Success(new InventoryItemResult(
            item.Sku,
            item.Name,
            item.StockBalance.OnHand - item.StockBalance.Reserved));
    }
}
