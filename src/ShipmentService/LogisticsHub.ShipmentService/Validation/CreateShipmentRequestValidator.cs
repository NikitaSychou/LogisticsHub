using FluentValidation;
using LogisticsHub.ShipmentService.Contracts;
using LogisticsHub.ShipmentService.Localization;
using Microsoft.Extensions.Localization;

namespace LogisticsHub.ShipmentService.Validation;

public sealed class CreateShipmentRequestValidator : AbstractValidator<CreateShipmentRequest>
{
    private readonly IStringLocalizer<ShipmentValidationMessages> _localizer;

    public CreateShipmentRequestValidator(IStringLocalizer<ShipmentValidationMessages> localizer)
    {
        _localizer = localizer;

        RuleFor(request => request).Custom(ValidateRequest);
    }

    private void ValidateRequest(
        CreateShipmentRequest request,
        ValidationContext<CreateShipmentRequest> context)
    {
        var missingReferenceFields = new List<string>();
        if (!request.SenderCompanyId.HasValue)
        {
            missingReferenceFields.Add("senderCompanyId");
        }

        if (!request.SenderAddressId.HasValue)
        {
            missingReferenceFields.Add("senderAddressId");
        }

        if (!request.ReceiverCompanyId.HasValue)
        {
            missingReferenceFields.Add("receiverCompanyId");
        }

        if (!request.ReceiverAddressId.HasValue)
        {
            missingReferenceFields.Add("receiverAddressId");
        }

        if (missingReferenceFields.Count > 0)
        {
            context.AddFailure(
                "companyAddressReferences",
                _localizer["shipment.company_address_references.required", string.Join(", ", missingReferenceFields)].Value
            );
        }

        if (request.Items is null)
        {
            context.AddFailure("items", _localizer["shipment.items.required"].Value);
            return;
        }

        if (request.Items.Count == 0)
        {
            context.AddFailure("items", _localizer["shipment.items.not_empty"].Value);
            return;
        }

        var seenSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var duplicateSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var index = 0;

        foreach (var item in request.Items)
        {
            if (item is null)
            {
                context.AddFailure($"items[{index}]", _localizer["shipment.item.required"].Value);
                index++;
                continue;
            }

            var sku = item.Sku?.Trim();

            if (string.IsNullOrWhiteSpace(sku))
            {
                context.AddFailure($"items[{index}].sku", _localizer["shipment.item.sku.required"].Value);
            }
            else if (!seenSkus.Add(sku))
            {
                duplicateSkus.Add(sku);
            }

            if (item.Quantity <= 0)
            {
                context.AddFailure($"items[{index}].quantity", _localizer["shipment.item.quantity.positive"].Value);
            }

            index++;
        }

        if (duplicateSkus.Count > 0)
        {
            context.AddFailure("items", _localizer["shipment.item.sku.duplicates", string.Join(", ", duplicateSkus)].Value);
        }
    }
}
