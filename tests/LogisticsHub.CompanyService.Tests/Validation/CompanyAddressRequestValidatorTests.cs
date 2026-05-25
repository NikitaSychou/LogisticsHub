using LogisticsHub.CompanyService.Contracts;
using LogisticsHub.CompanyService.Validation;
using Xunit;

namespace LogisticsHub.CompanyService.Tests.Validation;

public sealed class CompanyAddressRequestValidatorTests
{
    [Fact]
    public void Validate_WhenAddressTypeIsMissing_ReturnsAddressTypeError()
    {
        var errors = CompanyAddressRequestValidator.Validate(CreateRequest(addressType: " "));

        Assert.Contains("addressType", errors.Keys);
    }

    [Fact]
    public void Validate_WhenAddressTypeIsInvalid_ReturnsAddressTypeError()
    {
        var errors = CompanyAddressRequestValidator.Validate(CreateRequest(addressType: "Home"));

        Assert.Contains("addressType", errors.Keys);
    }

    [Fact]
    public void Validate_WhenCountryCodeIsMissing_ReturnsCountryCodeError()
    {
        var errors = CompanyAddressRequestValidator.Validate(CreateRequest(countryCode: " "));

        Assert.Contains("countryCode", errors.Keys);
    }

    [Theory]
    [InlineData("U")]
    [InlineData("USA")]
    public void Validate_WhenCountryCodeLengthIsNotTwo_ReturnsCountryCodeError(string countryCode)
    {
        var errors = CompanyAddressRequestValidator.Validate(CreateRequest(countryCode: countryCode));

        Assert.Contains("countryCode", errors.Keys);
    }

    [Fact]
    public void Validate_WhenCityIsMissing_ReturnsCityError()
    {
        var errors = CompanyAddressRequestValidator.Validate(CreateRequest(city: " "));

        Assert.Contains("city", errors.Keys);
    }

    [Fact]
    public void Validate_WhenCityIsTooLong_ReturnsCityError()
    {
        var errors = CompanyAddressRequestValidator.Validate(CreateRequest(city: new string('A', 101)));

        Assert.Contains("city", errors.Keys);
    }

    [Fact]
    public void Validate_WhenLine1IsMissing_ReturnsLine1Error()
    {
        var errors = CompanyAddressRequestValidator.Validate(CreateRequest(line1: " "));

        Assert.Contains("line1", errors.Keys);
    }

    [Fact]
    public void Validate_WhenLine1IsTooLong_ReturnsLine1Error()
    {
        var errors = CompanyAddressRequestValidator.Validate(CreateRequest(line1: new string('A', 201)));

        Assert.Contains("line1", errors.Keys);
    }

    [Fact]
    public void Validate_WhenPostalCodeIsTooLong_ReturnsPostalCodeError()
    {
        var errors = CompanyAddressRequestValidator.Validate(CreateRequest(postalCode: new string('A', 33)));

        Assert.Contains("postalCode", errors.Keys);
    }

    [Fact]
    public void Validate_WhenLine2IsTooLong_ReturnsLine2Error()
    {
        var errors = CompanyAddressRequestValidator.Validate(CreateRequest(line2: new string('A', 201)));

        Assert.Contains("line2", errors.Keys);
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
