namespace LogisticsHub.InventoryService.Contracts;

public sealed record CreateInventoryItemRequest(
    string Sku,
    string Name,
    int QuantityAvailable);
