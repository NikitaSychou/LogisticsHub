using LogisticsHub.ShipmentService.Contracts;

namespace LogisticsHub.ShipmentService.Validation;

public static class CreateShipmentRequestValidator
{
    public static Dictionary<string, string[]> Validate(CreateShipmentRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.Items is null)
        {
            errors["items"] = ["Items are required."];
            return errors;
        }

        if (request.Items.Count == 0)
        {
            errors["items"] = ["At least one shipment item is required."];
            return errors;
        }

        var seenSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var duplicateSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var index = 0;

        foreach (var item in request.Items)
        {
            if (item is null)
            {
                errors[$"items[{index}]"] = ["Shipment item is required."];
                index++;
                continue;
            }

            var sku = item.Sku?.Trim();

            if (string.IsNullOrWhiteSpace(sku))
            {
                errors[$"items[{index}].sku"] = ["SKU is required."];
            }
            else if (!seenSkus.Add(sku))
            {
                duplicateSkus.Add(sku);
            }

            if (item.Quantity <= 0)
            {
                errors[$"items[{index}].quantity"] = ["Quantity must be greater than zero."];
            }

            index++;
        }

        if (duplicateSkus.Count > 0)
        {
            errors["items"] = [$"Duplicate SKU values are not allowed: {string.Join(", ", duplicateSkus)}."];
        }

        return errors;
    }
}
