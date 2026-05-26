using LogisticsHub.ShipmentService.Contracts;
using LogisticsHub.ShipmentService.Validation;
using Xunit;

namespace LogisticsHub.ShipmentService.Application.Tests.Shipments;

public sealed class CreateShipmentRequestValidatorTests
{
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
        var errors = CreateShipmentRequestValidator.Validate(request);

        // Assert
        Assert.Contains("companyAddressReferences", errors.Keys);
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
        var errors = CreateShipmentRequestValidator.Validate(request);

        // Assert
        Assert.Contains("companyAddressReferences", errors.Keys);
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
        var errors = CreateShipmentRequestValidator.Validate(request);

        // Assert
        Assert.DoesNotContain("companyAddressReferences", errors.Keys);
    }
}
