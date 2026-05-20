namespace LogisticsHub.InventoryService.Application.StockReservations;

public sealed record StockReservationItemCommand(
    string Sku,
    int Quantity);
