namespace LogisticsHub.InventoryService.Contracts;

public sealed record CreateStockReservationRequest(
    Guid ShipmentId,
    IReadOnlyCollection<CreateStockReservationItemRequest>? Items);
