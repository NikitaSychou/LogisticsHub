using FluentValidation;
using LogisticsHub.InventoryService.Contracts;
using LogisticsHub.InventoryService.Localization;
using Microsoft.Extensions.Localization;

namespace LogisticsHub.InventoryService.Validation;

public sealed class CreateStockReservationRequestValidator : AbstractValidator<CreateStockReservationRequest>
{
    private readonly IStringLocalizer<InventoryValidationMessages> _localizer;

    public CreateStockReservationRequestValidator(IStringLocalizer<InventoryValidationMessages> localizer)
    {
        _localizer = localizer;

        RuleFor(request => request).Custom((request, context) =>
        {
            if (request.ShipmentId == Guid.Empty)
            {
                context.AddFailure(nameof(request.ShipmentId), _localizer["stock_reservation.shipment_id.required"].Value);
            }

            if (request.Items is null || request.Items.Count == 0)
            {
                context.AddFailure(nameof(request.Items), _localizer["stock_reservation.items.required"].Value);
                return;
            }

            ValidateItems(request.Items, context);
        });
    }

    private void ValidateItems(
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
                skuErrors.Add(_localizer["stock_reservation.item.sku.required"].Value);
                continue;
            }

            if (!skus.Add(item.Sku.Trim()))
            {
                skuErrors.Add(_localizer["stock_reservation.item.sku.duplicate", item.Sku].Value);
            }

            if (item.Quantity <= 0)
            {
                quantityErrors.Add(_localizer["stock_reservation.item.quantity.positive"].Value);
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
