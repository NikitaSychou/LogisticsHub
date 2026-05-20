using LogisticsHub.InventoryService.Domain.Enums;

namespace LogisticsHub.InventoryService.Application.StockReservations;

public sealed record StockReservationResult(
    Guid ReservationId,
    Guid ShipmentId,
    ReservationStatus Status,
    IReadOnlyCollection<StockReservationItemResult> Items);
