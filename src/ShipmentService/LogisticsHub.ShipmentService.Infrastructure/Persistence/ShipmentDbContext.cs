using LogisticsHub.ShipmentService.Application.Persistence;
using LogisticsHub.ShipmentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogisticsHub.ShipmentService.Infrastructure.Persistence;

public class ShipmentDbContext : DbContext, IShipmentDbContext
{
    private const string InboxEventIdIndexName = "IX_shipment_inbox_messages_event_id";

    public ShipmentDbContext(DbContextOptions<ShipmentDbContext> options) : base(options)
    {
    }

    public DbSet<Shipment> Shipments { get; set; }
    public DbSet<ShipmentItem> ShipmentItems { get; set; }
    public DbSet<ShipmentOutboxMessage> ShipmentOutboxMessages { get; set; }
    public DbSet<ShipmentInboxMessage> ShipmentInboxMessages { get; set; }

    public async Task<Shipment?> GetShipmentByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Shipments
            .AsNoTracking()
            .Include(shipment => shipment.Items)
            .SingleOrDefaultAsync(shipment => shipment.Id == id, cancellationToken);
    }

    public async Task<Shipment?> GetShipmentForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Shipments
            .SingleOrDefaultAsync(shipment => shipment.Id == id, cancellationToken);
    }

    public async Task AddShipmentAsync(Shipment shipment, CancellationToken cancellationToken = default)
    {
        await Shipments.AddAsync(shipment, cancellationToken);
    }

    public async Task AddShipmentItemAsync(ShipmentItem shipmentItem, CancellationToken cancellationToken = default)
    {
        await ShipmentItems.AddAsync(shipmentItem, cancellationToken);
    }

    public async Task AddShipmentOutboxMessageAsync(
        ShipmentOutboxMessage outboxMessage,
        CancellationToken cancellationToken = default)
    {
        await ShipmentOutboxMessages.AddAsync(outboxMessage, cancellationToken);
    }

    public async Task<IReadOnlyList<ShipmentOutboxMessage>> GetUnprocessedShipmentOutboxMessagesAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        return await ShipmentOutboxMessages
            .Where(message => message.ProcessedAtUtc == null)
            .OrderBy(message => message.OccurredAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasShipmentInboxMessageAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        return await ShipmentInboxMessages
            .AsNoTracking()
            .AnyAsync(message => message.EventId == eventId, cancellationToken);
    }

    public async Task AddShipmentInboxMessageAsync(
        ShipmentInboxMessage inboxMessage,
        CancellationToken cancellationToken = default)
    {
        await ShipmentInboxMessages.AddAsync(inboxMessage, cancellationToken);
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
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ShipmentDbContext).Assembly);
    }

    private static bool IsInboxEventIdUniqueIndexViolation(DbUpdateException exception)
    {
        return exception.ToString().Contains(InboxEventIdIndexName, StringComparison.OrdinalIgnoreCase);
    }
}
