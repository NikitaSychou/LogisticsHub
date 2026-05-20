namespace LogisticsHub.InventoryService.Contracts;

public sealed record CreateInventoryItemResponse(
    string Sku,
    string Name,
    int QuantityAvailable);
