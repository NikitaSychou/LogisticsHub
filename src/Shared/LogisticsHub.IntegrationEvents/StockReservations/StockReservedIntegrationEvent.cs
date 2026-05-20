namespace LogisticsHub.IntegrationEvents.StockReservations;

public sealed record StockReservedIntegrationEvent(
    Guid EventId,
    DateTime OccurredAtUtc,
    Guid ShipmentId,
    Guid ReservationId);
