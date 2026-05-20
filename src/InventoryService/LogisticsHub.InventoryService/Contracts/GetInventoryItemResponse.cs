namespace LogisticsHub.InventoryService.Contracts;

public sealed record GetInventoryItemResponse(
    string Sku,
    string Name,
    int QuantityAvailable);
