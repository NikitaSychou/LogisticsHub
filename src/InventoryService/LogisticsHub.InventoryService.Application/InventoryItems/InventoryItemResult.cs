namespace LogisticsHub.InventoryService.Application.InventoryItems;

public sealed record InventoryItemResult(
    string Sku,
    string Name,
    int QuantityAvailable);
