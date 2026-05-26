using LogisticsHub.ShipmentService.Application.Persistence;
using LogisticsHub.ShipmentService.Domain.Entities;

namespace LogisticsHub.ShipmentService.Application.Tests.Fakes;

public sealed class FakeShipmentDbContext : IShipmentDbContext
{
    public List<Shipment> Shipments { get; } = [];
    public List<ShipmentItem> ShipmentItems { get; } = [];
    public List<ShipmentInboxMessage> InboxMessages { get; } = [];
    public List<ShipmentOutboxMessage> OutboxMessages { get; } = [];

    public bool SaveChangesHandlingDuplicateInboxEventResult { get; set; } = true;
    public int SaveChangesCallCount { get; private set; }

    public Task<Shipment?> GetShipmentByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Shipments.SingleOrDefault(shipment => shipment.Id == id));
    }

    public Task<Shipment?> GetShipmentForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Shipments.SingleOrDefault(shipment => shipment.Id == id));
    }

    public Task AddShipmentAsync(Shipment shipment, CancellationToken cancellationToken = default)
    {
        Shipments.Add(shipment);
        return Task.CompletedTask;
    }

    public Task AddShipmentItemAsync(ShipmentItem shipmentItem, CancellationToken cancellationToken = default)
    {
        ShipmentItems.Add(shipmentItem);
        return Task.CompletedTask;
    }

    public Task AddShipmentOutboxMessageAsync(
        ShipmentOutboxMessage outboxMessage,
        CancellationToken cancellationToken = default)
    {
        OutboxMessages.Add(outboxMessage);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ShipmentOutboxMessage>> ClaimShipmentOutboxMessagesAsync(
        int batchSize,
        string lockedBy,
        DateTime lockedAtUtc,
        TimeSpan lockTimeout,
        CancellationToken cancellationToken = default)
    {
        var lockExpiresBefore = lockedAtUtc.Subtract(lockTimeout);
        var claimed = OutboxMessages
            .Where(message =>
                message.ProcessedAtUtc is null
                && message.FailedAtUtc is null
                && (message.NextAttemptAtUtc is null || message.NextAttemptAtUtc <= lockedAtUtc)
                && (message.LockedAtUtc is null || message.LockedAtUtc <= lockExpiresBefore))
            .OrderBy(message => message.OccurredAtUtc)
            .Take(batchSize)
            .ToArray();

        foreach (var message in claimed)
        {
            message.LockedBy = lockedBy;
            message.LockedAtUtc = lockedAtUtc;
        }

        return Task.FromResult<IReadOnlyList<ShipmentOutboxMessage>>(claimed);
    }

    public Task<bool> HasShipmentInboxMessageAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(InboxMessages.Any(message => message.EventId == eventId));
    }

    public Task AddShipmentInboxMessageAsync(
        ShipmentInboxMessage inboxMessage,
        CancellationToken cancellationToken = default)
    {
        InboxMessages.Add(inboxMessage);
        return Task.CompletedTask;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCallCount++;
        return Task.FromResult(1);
    }

    public Task<bool> SaveChangesAsyncHandlingDuplicateInboxEventAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(SaveChangesHandlingDuplicateInboxEventResult);
    }
}
