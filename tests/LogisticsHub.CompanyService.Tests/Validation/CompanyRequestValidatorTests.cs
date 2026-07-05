using LogisticsHub.CompanyService.Contracts;
using LogisticsHub.CompanyService.Localization;
using LogisticsHub.CompanyService.Validation;
using Microsoft.Extensions.Localization;
using Xunit;

namespace LogisticsHub.CompanyService.Tests.Validation;

public sealed class CompanyRequestValidatorTests
{
    private readonly CreateCompanyRequestValidator _validator = new(new FakeCompanyValidationLocalizer());

    [Fact]
    public void Validate_WhenNameIsMissing_ReturnsNameError()
    {
        var errors = _validator.Validate(CreateRequest(name: " "));

        Assert.Contains(errors.Errors, error => error.PropertyName == "name");
        Assert.Contains(errors.Errors, error => error.ErrorMessage == "Name is required.");
    }

    [Fact]
    public void Validate_WhenNameIsTooLong_ReturnsNameError()
    {
        var errors = _validator.Validate(CreateRequest(name: new string('A', 201)));

        Assert.Contains(errors.Errors, error => error.PropertyName == "name");
    }

    [Fact]
    public void Validate_WhenExternalCodeIsTooLong_ReturnsExternalCodeError()
    {
        var errors = _validator.Validate(CreateRequest(externalCode: new string('A', 65)));

        Assert.Contains(errors.Errors, error => error.PropertyName == "externalCode");
    }

    [Fact]
    public void Validate_WhenStatusIsInvalid_ReturnsStatusError()
    {
        var errors = _validator.Validate(CreateRequest(status: "Archived"));

        Assert.Contains(errors.Errors, error => error.PropertyName == "status");
    }

    [Fact]
    public void Validate_WhenStatusIsMissing_ReturnsStatusError()
    {
        var errors = _validator.Validate(CreateRequest(status: " "));

        Assert.Contains(errors.Errors, error => error.PropertyName == "status");
    }

    private static CreateCompanyRequest CreateRequest(
        string? name = "Acme",
        string? externalCode = "ACME",
        string? status = "Active")
    {
        return new CreateCompanyRequest(name, externalCode, status);
    }

    private sealed class FakeCompanyValidationLocalizer : IStringLocalizer<CompanyValidationMessages>
    {
        private static readonly IReadOnlyDictionary<string, string> Messages = new Dictionary<string, string>
        {
            ["company.name.required"] = "Name is required.",
            ["company.name.max_length"] = "Name must be 200 characters or fewer.",
            ["company.external_code.max_length"] = "External code must be 64 characters or fewer.",
            ["company.status.required"] = "Status is required.",
            ["company.status.invalid"] = "Status must be Active or Inactive."
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
