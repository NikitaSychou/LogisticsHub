using FluentValidation;
using LogisticsHub.InventoryService.Contracts;

namespace LogisticsHub.InventoryService.Validation;

public sealed class CreateStockReservationRequestValidator : AbstractValidator<CreateStockReservationRequest>
{
    public CreateStockReservationRequestValidator()
    {
        RuleFor(request => request).Custom((request, context) =>
        {
            if (request.ShipmentId == Guid.Empty)
            {
                context.AddFailure(nameof(request.ShipmentId), "Shipment ID is required.");
            }

            if (request.Items is null || request.Items.Count == 0)
            {
                context.AddFailure(nameof(request.Items), "At least one item is required.");
                return;
            }

            ValidateItems(request.Items, context);
        });
    }

    private static void ValidateItems(
        IReadOnlyCollection<CreateStockReservationItemRequest> items,
        ValidationContext<CreateStockReservationRequest> context)
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
            foreach (var error in skuErrors)
            {
                context.AddFailure(nameof(CreateStockReservationItemRequest.Sku), error);
            }
        }

        if (quantityErrors.Count > 0)
        {
            foreach (var error in quantityErrors)
            {
                context.AddFailure(nameof(CreateStockReservationItemRequest.Quantity), error);
            }
        }
    }
}
