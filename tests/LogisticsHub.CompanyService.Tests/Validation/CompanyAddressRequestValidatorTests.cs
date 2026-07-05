using LogisticsHub.CompanyService.Contracts;
using LogisticsHub.CompanyService.Localization;
using LogisticsHub.CompanyService.Validation;
using Microsoft.Extensions.Localization;
using Xunit;

namespace LogisticsHub.CompanyService.Tests.Validation;

public sealed class CompanyAddressRequestValidatorTests
{
    private readonly CompanyAddressRequestValidator _validator = new(new FakeCompanyValidationLocalizer());

    [Fact]
    public void Validate_WhenAddressTypeIsMissing_ReturnsAddressTypeError()
    {
        var errors = _validator.Validate(CreateRequest(addressType: " "));

        Assert.Contains(errors.Errors, error => error.PropertyName == "addressType");
        Assert.Contains(errors.Errors, error => error.ErrorMessage == "Address type is required.");
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

    private sealed class FakeCompanyValidationLocalizer : IStringLocalizer<CompanyValidationMessages>
    {
        private static readonly IReadOnlyDictionary<string, string> Messages = new Dictionary<string, string>
        {
            ["company_address.address_type.required"] = "Address type is required.",
            ["company_address.address_type.invalid"] = "Address type must be Legal, Billing, Shipping, or Warehouse.",
            ["company_address.country_code.required"] = "Country code is required.",
            ["company_address.country_code.exact_length"] = "Country code must be exactly 2 characters.",
            ["company_address.city.required"] = "City is required.",
            ["company_address.city.max_length"] = "City must be 100 characters or fewer.",
            ["company_address.postal_code.max_length"] = "Postal code must be 32 characters or fewer.",
            ["company_address.line1.required"] = "Line1 is required.",
            ["company_address.line1.max_length"] = "Line1 must be 200 characters or fewer.",
            ["company_address.line2.max_length"] = "Line2 must be 200 characters or fewer."
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
