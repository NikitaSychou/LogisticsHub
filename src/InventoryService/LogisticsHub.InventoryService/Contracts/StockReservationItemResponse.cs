namespace LogisticsHub.InventoryService.Contracts;

public sealed record StockReservationItemResponse(
    string Sku,
    int Quantity);
