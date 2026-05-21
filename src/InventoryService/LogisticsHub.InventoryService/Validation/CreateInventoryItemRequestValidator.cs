using LogisticsHub.InventoryService.Contracts;

namespace LogisticsHub.InventoryService.Validation;

public static class CreateInventoryItemRequestValidator
{
    public static Dictionary<string, string[]> Validate(CreateInventoryItemRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Sku))
        {
            errors[nameof(request.Sku)] = ["SKU is required."];
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors[nameof(request.Name)] = ["Name is required."];
        }

        if (request.QuantityAvailable < 0)
        {
            errors[nameof(request.QuantityAvailable)] = ["Quantity available must be zero or greater."];
        }

        return errors;
    }
}
