using FluentValidation;
using LogisticsHub.InventoryService.Contracts;

namespace LogisticsHub.InventoryService.Validation;

public sealed class CreateInventoryItemRequestValidator : AbstractValidator<CreateInventoryItemRequest>
{
    public CreateInventoryItemRequestValidator()
    {
        RuleFor(request => request.Sku)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("SKU is required.")
            .OverridePropertyName(nameof(CreateInventoryItemRequest.Sku));

        RuleFor(request => request.Name)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Name is required.")
            .OverridePropertyName(nameof(CreateInventoryItemRequest.Name));

        RuleFor(request => request.QuantityAvailable)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Quantity available must be zero or greater.")
            .OverridePropertyName(nameof(CreateInventoryItemRequest.QuantityAvailable));
    }
}
