using LogisticsHub.InventoryService.Application.Persistence;
using LogisticsHub.InventoryService.Domain.Entities;

namespace LogisticsHub.InventoryService.Application.Tests.Fakes;

public sealed class FakeInventoryDbContext : IInventoryDbContext
{
    public List<Item> Items { get; } = [];
    public List<StockReservation> StockReservations { get; } = [];
    public List<InventoryInboxMessage> InboxMessages { get; } = [];
    public List<InventoryOutboxMessage> OutboxMessages { get; } = [];

    public InventorySaveChangesResult SaveChangesResult { get; set; } = InventorySaveChangesResult.Saved;

    public Task<Item?> GetItemBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Items.SingleOrDefault(item => item.Sku == sku));
    }

    public Task<IReadOnlyList<Item>> GetItemsBySkusAsync(
        IReadOnlyCollection<string> skus,
        CancellationToken cancellationToken = default)
    {
        var results = Items
            .Where(item => skus.Contains(item.Sku))
            .ToArray();

        return Task.FromResult<IReadOnlyList<Item>>(results);
    }

    public Task<StockReservation?> GetStockReservationByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(StockReservations.SingleOrDefault(reservation => reservation.Id == id));
    }

    public Task<bool> HasInventoryInboxMessageAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(InboxMessages.Any(message => message.EventId == eventId));
    }

    public Task AddItemAsync(Item item, CancellationToken cancellationToken = default)
    {
        Items.Add(item);
        return Task.CompletedTask;
    }

    public Task AddStockBalanceAsync(StockBalance stockBalance, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task AddStockReservationAsync(
        StockReservation stockReservation,
        CancellationToken cancellationToken = default)
    {
        StockReservations.Add(stockReservation);
        return Task.CompletedTask;
    }

    public Task AddInventoryInboxMessageAsync(
        InventoryInboxMessage inboxMessage,
        CancellationToken cancellationToken = default)
    {
        InboxMessages.Add(inboxMessage);
        return Task.CompletedTask;
    }

    public Task AddInventoryOutboxMessageAsync(
        InventoryOutboxMessage outboxMessage,
        CancellationToken cancellationToken = default)
    {
        OutboxMessages.Add(outboxMessage);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<InventoryOutboxMessage>> ClaimInventoryOutboxMessagesAsync(
        int batchSize,
        string lockedBy,
        DateTime lockedAtUtc,
        TimeSpan lockTimeout,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<InventoryOutboxMessage>>([]);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(1);
    }

    public Task<InventorySaveChangesResult> SaveChangesAsyncHandlingDuplicateInboxEventAndConcurrencyAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(SaveChangesResult);
    }
}
