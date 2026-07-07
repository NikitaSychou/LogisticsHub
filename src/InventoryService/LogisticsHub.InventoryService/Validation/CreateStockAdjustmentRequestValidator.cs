using FluentValidation;
using LogisticsHub.InventoryService.Contracts;
using LogisticsHub.InventoryService.Localization;
using Microsoft.Extensions.Localization;

namespace LogisticsHub.InventoryService.Validation;

public sealed class CreateStockAdjustmentRequestValidator : AbstractValidator<CreateStockAdjustmentRequest>
{
    public CreateStockAdjustmentRequestValidator(IStringLocalizer<InventoryValidationMessages> localizer)
    {
        RuleFor(request => request.Quantity)
            .GreaterThan(0)
            .WithMessage(_ => localizer["inventory_item.stock_adjustment.quantity.positive"].Value)
            .OverridePropertyName(nameof(CreateStockAdjustmentRequest.Quantity));
    }
}
