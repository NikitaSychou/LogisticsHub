namespace LogisticsHub.InventoryService.Application.StockReservations;

public sealed record StockReservationItemResult(
    string Sku,
    int Quantity);
