using LogisticsHub.ShipmentService.Contracts;
using LogisticsHub.ShipmentService.Validation;
using Xunit;

namespace LogisticsHub.ShipmentService.Application.Tests.Shipments;

public sealed class CreateShipmentRequestValidatorTests
{
    private readonly CreateShipmentRequestValidator _validator = new();

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
}
