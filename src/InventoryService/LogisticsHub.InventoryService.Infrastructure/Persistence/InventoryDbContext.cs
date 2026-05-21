using LogisticsHub.InventoryService.Application.Persistence;
using LogisticsHub.InventoryService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogisticsHub.InventoryService.Infrastructure.Persistence;

public class InventoryDbContext : DbContext, IInventoryDbContext
{
    private const string InboxEventIdIndexName = "IX_inventory_inbox_messages_event_id";

    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options)
    {
    }

    public DbSet<Item> Items { get; set; }
    public DbSet<StockBalance> StockBalances { get; set; }
    public DbSet<StockReservation> StockReservations { get; set; }
    public DbSet<StockReservationItem> StockReservationItems { get; set; }
    public DbSet<InventoryInboxMessage> InventoryInboxMessages { get; set; }
    public DbSet<InventoryOutboxMessage> InventoryOutboxMessages { get; set; }

    public async Task<Item?> GetItemBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        return await Items
            .AsNoTracking()
            .Include(item => item.StockBalance)
            .SingleOrDefaultAsync(item => item.Sku == sku, cancellationToken);
    }

    public async Task<IReadOnlyList<Item>> GetItemsBySkusAsync(
        IReadOnlyCollection<string> skus,
        CancellationToken cancellationToken = default)
    {
        return await Items
            .Include(item => item.StockBalance)
            .Where(item => skus.Contains(item.Sku))
            .ToListAsync(cancellationToken);
    }

    public async Task<StockReservation?> GetStockReservationByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await StockReservations
            .AsNoTracking()
            .Include(stockReservation => stockReservation.Items)
            .ThenInclude(stockReservationItem => stockReservationItem.Item)
            .SingleOrDefaultAsync(stockReservation => stockReservation.Id == id, cancellationToken);
    }

    public async Task<bool> HasInventoryInboxMessageAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        return await InventoryInboxMessages
            .AsNoTracking()
            .AnyAsync(message => message.EventId == eventId, cancellationToken);
    }

    public async Task AddItemAsync(Item item, CancellationToken cancellationToken = default)
    {
        await Items.AddAsync(item, cancellationToken);
    }

    public async Task AddStockBalanceAsync(StockBalance stockBalance, CancellationToken cancellationToken = default)
    {
        await StockBalances.AddAsync(stockBalance, cancellationToken);
    }

    public async Task AddStockReservationAsync(
        StockReservation stockReservation,
        CancellationToken cancellationToken = default)
    {
        await StockReservations.AddAsync(stockReservation, cancellationToken);
    }

    public async Task AddInventoryInboxMessageAsync(
        InventoryInboxMessage inboxMessage,
        CancellationToken cancellationToken = default)
    {
        await InventoryInboxMessages.AddAsync(inboxMessage, cancellationToken);
    }

    public async Task AddInventoryOutboxMessageAsync(
        InventoryOutboxMessage outboxMessage,
        CancellationToken cancellationToken = default)
    {
        await InventoryOutboxMessages.AddAsync(outboxMessage, cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryOutboxMessage>> GetUnprocessedInventoryOutboxMessagesAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        return await InventoryOutboxMessages
            .Where(message => message.ProcessedAtUtc == null)
            .OrderBy(message => message.OccurredAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> SaveChangesAsyncHandlingDuplicateInboxEventAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            await SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (IsInboxEventIdUniqueIndexViolation(exception))
        {
            return false;
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);
    }

    private static bool IsInboxEventIdUniqueIndexViolation(DbUpdateException exception)
    {
        return exception.ToString().Contains(InboxEventIdIndexName, StringComparison.OrdinalIgnoreCase);
    }
}
