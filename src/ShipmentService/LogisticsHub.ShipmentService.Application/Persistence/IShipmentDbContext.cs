using LogisticsHub.ShipmentService.Domain.Entities;

namespace LogisticsHub.ShipmentService.Application.Persistence;

public interface IShipmentDbContext
{
    Task<Shipment?> GetShipmentByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Shipment?> GetShipmentForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddShipmentAsync(Shipment shipment, CancellationToken cancellationToken = default);

    Task AddShipmentItemAsync(ShipmentItem shipmentItem, CancellationToken cancellationToken = default);

    Task AddShipmentOutboxMessageAsync(ShipmentOutboxMessage outboxMessage, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShipmentOutboxMessage>> ClaimShipmentOutboxMessagesAsync(
        int batchSize,
        string lockedBy,
        DateTime lockedAtUtc,
        TimeSpan lockTimeout,
        CancellationToken cancellationToken = default);

    Task<bool> HasShipmentInboxMessageAsync(Guid eventId, CancellationToken cancellationToken = default);

    Task AddShipmentInboxMessageAsync(ShipmentInboxMessage inboxMessage, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<bool> SaveChangesAsyncHandlingDuplicateInboxEventAsync(CancellationToken cancellationToken = default);
}
