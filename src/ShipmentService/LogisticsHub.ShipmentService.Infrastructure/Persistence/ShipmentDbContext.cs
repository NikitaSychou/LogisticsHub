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

    public async Task<IReadOnlyList<ShipmentOutboxMessage>> ClaimShipmentOutboxMessagesAsync(
        int batchSize,
        string lockedBy,
        DateTime lockedAtUtc,
        TimeSpan lockTimeout,
        CancellationToken cancellationToken = default)
    {
        var lockExpiresBefore = lockedAtUtc.Subtract(lockTimeout);

        await Database.ExecuteSqlInterpolatedAsync(
            BuildClaimShipmentOutboxMessagesSql(batchSize, lockedBy, lockedAtUtc, lockExpiresBefore),
            cancellationToken);

        return await ShipmentOutboxMessages
            .Where(message =>
                message.ProcessedAtUtc == null
                && message.LockedBy == lockedBy
                && message.LockedAtUtc == lockedAtUtc)
            .OrderBy(message => message.OccurredAtUtc)
            .ToListAsync(cancellationToken);
    }

    private static FormattableString BuildClaimShipmentOutboxMessagesSql(
        int batchSize,
        string lockedBy,
        DateTime lockedAtUtc,
        DateTime lockExpiresBefore)
    {
        // Raw SQL is used for atomic multi-replica row claiming with SQL Server lock hints.
        // EF LINQ/ExecuteUpdate cannot safely express this ordered TOP update with UPDLOCK/READPAST/ROWLOCK.
        return $"""
            ;WITH messages AS
            (
                SELECT TOP({batchSize}) *
                FROM dbo.shipment_outbox_messages WITH (UPDLOCK, READPAST, ROWLOCK)
                WHERE processed_at_utc IS NULL
                    AND failed_at_utc IS NULL
                    AND (next_attempt_at_utc IS NULL OR next_attempt_at_utc <= {lockedAtUtc})
                    AND (locked_at_utc IS NULL OR locked_at_utc < {lockExpiresBefore})
                ORDER BY occurred_at_utc
            )
            UPDATE messages
            SET locked_by = {lockedBy},
                locked_at_utc = {lockedAtUtc}
            """;
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
