using LogisticsHub.CompanyService.Contracts;
using LogisticsHub.CompanyService.Validation;
using Xunit;

namespace LogisticsHub.CompanyService.Tests.Validation;

public sealed class CompanyRequestValidatorTests
{
    private readonly CreateCompanyRequestValidator _validator = new();

    [Fact]
    public void Validate_WhenNameIsMissing_ReturnsNameError()
    {
        var errors = _validator.Validate(CreateRequest(name: " "));

        Assert.Contains(errors.Errors, error => error.PropertyName == "name");
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
}
