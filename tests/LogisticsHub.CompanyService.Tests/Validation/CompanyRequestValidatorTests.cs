using LogisticsHub.CompanyService.Contracts;
using LogisticsHub.CompanyService.Validation;
using Xunit;

namespace LogisticsHub.CompanyService.Tests.Validation;

public sealed class CompanyRequestValidatorTests
{
    [Fact]
    public void Validate_WhenNameIsMissing_ReturnsNameError()
    {
        var errors = CompanyRequestValidator.Validate(CreateRequest(name: " "));

        Assert.Contains("name", errors.Keys);
    }

    [Fact]
    public void Validate_WhenNameIsTooLong_ReturnsNameError()
    {
        var errors = CompanyRequestValidator.Validate(CreateRequest(name: new string('A', 201)));

        Assert.Contains("name", errors.Keys);
    }

    [Fact]
    public void Validate_WhenExternalCodeIsTooLong_ReturnsExternalCodeError()
    {
        var errors = CompanyRequestValidator.Validate(CreateRequest(externalCode: new string('A', 65)));

        Assert.Contains("externalCode", errors.Keys);
    }

    [Fact]
    public void Validate_WhenStatusIsInvalid_ReturnsStatusError()
    {
        var errors = CompanyRequestValidator.Validate(CreateRequest(status: "Archived"));

        Assert.Contains("status", errors.Keys);
    }

    [Fact]
    public void Validate_WhenStatusIsMissing_ReturnsStatusError()
    {
        var errors = CompanyRequestValidator.Validate(CreateRequest(status: " "));

        Assert.Contains("status", errors.Keys);
    }

    private static CreateCompanyRequest CreateRequest(
        string? name = "Acme",
        string? externalCode = "ACME",
        string? status = "Active")
    {
        return new CreateCompanyRequest(name, externalCode, status);
    }
}
