using LogisticsHub.InventoryService.Domain.Enums;

namespace LogisticsHub.InventoryService.Contracts;

public sealed record CreateStockReservationResponse(
    Guid ReservationId,
    Guid ShipmentId,
    ReservationStatus Status,
    IReadOnlyCollection<StockReservationItemResponse> Items);
