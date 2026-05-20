namespace LogisticsHub.IntegrationEvents.StockReservations;

public sealed record StockReservationRequestedIntegrationEvent(
    Guid EventId,
    DateTime OccurredAtUtc,
    Guid ShipmentId,
    IReadOnlyCollection<StockReservationRequestedItem> Items);
