namespace LogisticsHub.IntegrationEvents.StockReservations;

public sealed record StockReservationFailedIntegrationEvent(
    Guid EventId,
    DateTime OccurredAtUtc,
    Guid ShipmentId,
    string Reason);
