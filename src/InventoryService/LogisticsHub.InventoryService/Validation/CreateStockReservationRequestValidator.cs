using LogisticsHub.InventoryService.Contracts;

namespace LogisticsHub.InventoryService.Validation;

public static class CreateStockReservationRequestValidator
{
    public static Dictionary<string, string[]> Validate(CreateStockReservationRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.ShipmentId == Guid.Empty)
        {
            errors[nameof(request.ShipmentId)] = ["Shipment ID is required."];
        }

        if (request.Items is null || request.Items.Count == 0)
        {
            errors[nameof(request.Items)] = ["At least one item is required."];
            return errors;
        }

        ValidateItems(request.Items, errors);

        return errors;
    }

    private static void ValidateItems(
        IReadOnlyCollection<CreateStockReservationItemRequest> items,
        Dictionary<string, string[]> errors)
    {
        var skus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var skuErrors = new List<string>();
        var quantityErrors = new List<string>();

        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Sku))
            {
                skuErrors.Add("SKU is required.");
                continue;
            }

            if (!skus.Add(item.Sku.Trim()))
            {
                skuErrors.Add($"Duplicate SKU '{item.Sku}' is not allowed.");
            }

            if (item.Quantity <= 0)
            {
                quantityErrors.Add("Quantity must be greater than zero.");
            }
        }

        if (skuErrors.Count > 0)
        {
            errors[nameof(CreateStockReservationItemRequest.Sku)] = skuErrors.ToArray();
        }

        if (quantityErrors.Count > 0)
        {
            errors[nameof(CreateStockReservationItemRequest.Quantity)] = quantityErrors.ToArray();
        }
    }
}
