using LogisticsHub.InventoryService.Domain.Entities;

namespace LogisticsHub.InventoryService.Application.Persistence;

public interface IInventoryDbContext
{
    Task<Item?> GetItemBySkuAsync(string sku, CancellationToken cancellationToken = default);

    Task<Item?> GetItemForUpdateBySkuAsync(string sku, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Item>> GetItemsBySkusAsync(
        IReadOnlyCollection<string> skus,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Item>> ListItemsPageAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<StockReservation?> GetStockReservationByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> HasInventoryInboxMessageAsync(Guid eventId, CancellationToken cancellationToken = default);

    Task AddItemAsync(Item item, CancellationToken cancellationToken = default);

    Task AddStockBalanceAsync(StockBalance stockBalance, CancellationToken cancellationToken = default);

    Task AddStockReservationAsync(StockReservation stockReservation, CancellationToken cancellationToken = default);

    Task AddInventoryInboxMessageAsync(
        InventoryInboxMessage inboxMessage,
        CancellationToken cancellationToken = default);

    Task AddInventoryOutboxMessageAsync(
        InventoryOutboxMessage outboxMessage,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryOutboxMessage>> ClaimInventoryOutboxMessagesAsync(
        int batchSize,
        string lockedBy,
        DateTime lockedAtUtc,
        TimeSpan lockTimeout,
        CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<InventorySaveChangesResult> SaveChangesAsyncHandlingDuplicateInboxEventAndConcurrencyAsync(
        CancellationToken cancellationToken = default);
}
