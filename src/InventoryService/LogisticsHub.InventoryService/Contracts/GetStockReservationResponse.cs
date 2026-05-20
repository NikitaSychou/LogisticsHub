using LogisticsHub.InventoryService.Domain.Enums;

namespace LogisticsHub.InventoryService.Contracts;

public sealed record GetStockReservationResponse(
    Guid ReservationId,
    Guid ShipmentId,
    ReservationStatus Status,
    IReadOnlyCollection<StockReservationItemResponse> Items);
