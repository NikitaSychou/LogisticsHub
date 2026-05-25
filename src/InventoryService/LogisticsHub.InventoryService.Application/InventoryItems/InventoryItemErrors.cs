using LogisticsHub.Results;

namespace LogisticsHub.InventoryService.Application.InventoryItems;

public static class InventoryItemErrors
{
    public static Error NotFound(string sku)
    {
        return new Error(
            "inventory.item.not_found",
            "Inventory item was not found.",
            new Dictionary<string, object?> { ["sku"] = sku });
    }

    public static Error AlreadyExists(string sku)
    {
        return new Error(
            "inventory.item.already_exists",
            "Inventory item already exists.",
            new Dictionary<string, object?> { ["sku"] = sku });
    }
}
