using LogisticsHub.ShipmentService.Contracts;
using LogisticsHub.ShipmentService.Localization;
using LogisticsHub.ShipmentService.Validation;
using Microsoft.Extensions.Localization;
using Xunit;

namespace LogisticsHub.ShipmentService.Application.Tests.Shipments;

public sealed class CreateShipmentRequestValidatorTests
{
    private readonly CreateShipmentRequestValidator _validator = new(new FakeShipmentValidationLocalizer());

    [Fact]
    public void Validate_WhenCompanyAddressReferencesAreOmitted_ReturnsReferenceError()
    {
        // Arrange
        var request = new CreateShipmentRequest(
            [new CreateShipmentItemRequest("TEST-SKU-001", 1)],
            SenderCompanyId: null,
            SenderAddressId: null,
            ReceiverCompanyId: null,
            ReceiverAddressId: null);

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.Contains(result.Errors, error => error.PropertyName == "companyAddressReferences");
        Assert.Contains(
            result.Errors,
            error => error.ErrorMessage ==
                "Sender company, sender address, receiver company, and receiver address are required. Missing: senderCompanyId, senderAddressId, receiverCompanyId, receiverAddressId.");
    }

    [Fact]
    public void Validate_WhenCompanyAddressReferencesArePartial_ReturnsReferenceError()
    {
        // Arrange
        var request = new CreateShipmentRequest(
            [new CreateShipmentItemRequest("TEST-SKU-001", 1)],
            SenderCompanyId: Guid.NewGuid(),
            SenderAddressId: null,
            ReceiverCompanyId: null,
            ReceiverAddressId: null);

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.Contains(result.Errors, error => error.PropertyName == "companyAddressReferences");
    }

    [Fact]
    public void Validate_WhenAllCompanyAddressReferencesAreProvided_ReturnsNoReferenceErrors()
    {
        // Arrange
        var request = new CreateShipmentRequest(
            [new CreateShipmentItemRequest("TEST-SKU-001", 1)],
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid());

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.DoesNotContain(result.Errors, error => error.PropertyName == "companyAddressReferences");
    }

    private sealed class FakeShipmentValidationLocalizer : IStringLocalizer<ShipmentValidationMessages>
    {
        private static readonly IReadOnlyDictionary<string, string> Messages = new Dictionary<string, string>
        {
            ["shipment.company_address_references.required"] =
                "Sender company, sender address, receiver company, and receiver address are required. Missing: {0}.",
            ["shipment.items.required"] = "Items are required.",
            ["shipment.items.not_empty"] = "At least one shipment item is required.",
            ["shipment.item.required"] = "Shipment item is required.",
            ["shipment.item.sku.required"] = "SKU is required.",
            ["shipment.item.quantity.positive"] = "Quantity must be greater than zero.",
            ["shipment.item.sku.duplicates"] = "Duplicate SKU values are not allowed: {0}."
        };

        public LocalizedString this[string name] => new(name, Messages[name]);

        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(Messages[name], arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            return [];
        }
    }
}
