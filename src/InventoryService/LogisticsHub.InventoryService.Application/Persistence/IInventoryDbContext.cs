using LogisticsHub.InventoryService.Domain.Entities;

namespace LogisticsHub.InventoryService.Application.Persistence;

public interface IInventoryDbContext
{
    Task<Item?> GetItemBySkuAsync(string sku, CancellationToken cancellationToken = default);

    Task AddItemAsync(Item item, CancellationToken cancellationToken = default);

    Task AddStockBalanceAsync(StockBalance stockBalance, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
