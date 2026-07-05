using FluentValidation;
using LogisticsHub.ShipmentService.Contracts;

namespace LogisticsHub.ShipmentService.Validation;

public sealed class CreateShipmentRequestValidator : AbstractValidator<CreateShipmentRequest>
{
    public CreateShipmentRequestValidator()
    {
        RuleFor(request => request).Custom(ValidateRequest);
    }

    private static void ValidateRequest(
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
                $"Sender company, sender address, receiver company, and receiver address are required. Missing: {string.Join(", ", missingReferenceFields)}."
            );
        }

        if (request.Items is null)
        {
            context.AddFailure("items", "Items are required.");
            return;
        }

        if (request.Items.Count == 0)
        {
            context.AddFailure("items", "At least one shipment item is required.");
            return;
        }

        var seenSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var duplicateSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var index = 0;

        foreach (var item in request.Items)
        {
            if (item is null)
            {
                context.AddFailure($"items[{index}]", "Shipment item is required.");
                index++;
                continue;
            }

            var sku = item.Sku?.Trim();

            if (string.IsNullOrWhiteSpace(sku))
            {
                context.AddFailure($"items[{index}].sku", "SKU is required.");
            }
            else if (!seenSkus.Add(sku))
            {
                duplicateSkus.Add(sku);
            }

            if (item.Quantity <= 0)
            {
                context.AddFailure($"items[{index}].quantity", "Quantity must be greater than zero.");
            }

            index++;
        }

        if (duplicateSkus.Count > 0)
        {
            context.AddFailure("items", $"Duplicate SKU values are not allowed: {string.Join(", ", duplicateSkus)}.");
        }
    }
}
