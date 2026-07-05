using LogisticsHub.CompanyService.Contracts;
using LogisticsHub.CompanyService.Validation;
using Xunit;

namespace LogisticsHub.CompanyService.Tests.Validation;

public sealed class CompanyAddressRequestValidatorTests
{
    private readonly CompanyAddressRequestValidator _validator = new();

    [Fact]
    public void Validate_WhenAddressTypeIsMissing_ReturnsAddressTypeError()
    {
        var errors = _validator.Validate(CreateRequest(addressType: " "));

        Assert.Contains(errors.Errors, error => error.PropertyName == "addressType");
    }

    [Fact]
    public void Validate_WhenAddressTypeIsInvalid_ReturnsAddressTypeError()
    {
        var errors = _validator.Validate(CreateRequest(addressType: "Home"));

        Assert.Contains(errors.Errors, error => error.PropertyName == "addressType");
    }

    [Fact]
    public void Validate_WhenCountryCodeIsMissing_ReturnsCountryCodeError()
    {
        var errors = _validator.Validate(CreateRequest(countryCode: " "));

        Assert.Contains(errors.Errors, error => error.PropertyName == "countryCode");
    }

    [Theory]
    [InlineData("U")]
    [InlineData("USA")]
    public void Validate_WhenCountryCodeLengthIsNotTwo_ReturnsCountryCodeError(string countryCode)
    {
        var errors = _validator.Validate(CreateRequest(countryCode: countryCode));

        Assert.Contains(errors.Errors, error => error.PropertyName == "countryCode");
    }

    [Fact]
    public void Validate_WhenCityIsMissing_ReturnsCityError()
    {
        var errors = _validator.Validate(CreateRequest(city: " "));

        Assert.Contains(errors.Errors, error => error.PropertyName == "city");
    }

    [Fact]
    public void Validate_WhenCityIsTooLong_ReturnsCityError()
    {
        var errors = _validator.Validate(CreateRequest(city: new string('A', 101)));

        Assert.Contains(errors.Errors, error => error.PropertyName == "city");
    }

    [Fact]
    public void Validate_WhenLine1IsMissing_ReturnsLine1Error()
    {
        var errors = _validator.Validate(CreateRequest(line1: " "));

        Assert.Contains(errors.Errors, error => error.PropertyName == "line1");
    }

    [Fact]
    public void Validate_WhenLine1IsTooLong_ReturnsLine1Error()
    {
        var errors = _validator.Validate(CreateRequest(line1: new string('A', 201)));

        Assert.Contains(errors.Errors, error => error.PropertyName == "line1");
    }

    [Fact]
    public void Validate_WhenPostalCodeIsTooLong_ReturnsPostalCodeError()
    {
        var errors = _validator.Validate(CreateRequest(postalCode: new string('A', 33)));

        Assert.Contains(errors.Errors, error => error.PropertyName == "postalCode");
    }

    [Fact]
    public void Validate_WhenLine2IsTooLong_ReturnsLine2Error()
    {
        var errors = _validator.Validate(CreateRequest(line2: new string('A', 201)));

        Assert.Contains(errors.Errors, error => error.PropertyName == "line2");
    }

    private static CreateCompanyAddressRequest CreateRequest(
        string? addressType = "Shipping",
        string? countryCode = "US",
        string? city = "New York",
        string? postalCode = "10001",
        string? line1 = "1 Logistics Way",
        string? line2 = null)
    {
        return new CreateCompanyAddressRequest(addressType, countryCode, city, postalCode, line1, line2);
    }
}
