namespace LogisticsHub.InventoryService.Contracts;

public sealed record CreateStockReservationItemRequest(
    string? Sku,
    int Quantity);
