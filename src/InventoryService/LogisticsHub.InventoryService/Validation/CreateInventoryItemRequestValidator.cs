using FluentValidation;
using LogisticsHub.InventoryService.Contracts;
using LogisticsHub.InventoryService.Localization;
using Microsoft.Extensions.Localization;

namespace LogisticsHub.InventoryService.Validation;

public sealed class CreateInventoryItemRequestValidator : AbstractValidator<CreateInventoryItemRequest>
{
    public CreateInventoryItemRequestValidator(IStringLocalizer<InventoryValidationMessages> localizer)
    {
        RuleFor(request => request.Sku)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage(_ => localizer["inventory_item.sku.required"].Value)
            .OverridePropertyName(nameof(CreateInventoryItemRequest.Sku));

        RuleFor(request => request.Name)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage(_ => localizer["inventory_item.name.required"].Value)
            .OverridePropertyName(nameof(CreateInventoryItemRequest.Name));

        RuleFor(request => request.QuantityAvailable)
            .GreaterThanOrEqualTo(0)
            .WithMessage(_ => localizer["inventory_item.quantity_available.non_negative"].Value)
            .OverridePropertyName(nameof(CreateInventoryItemRequest.QuantityAvailable));
    }
}
