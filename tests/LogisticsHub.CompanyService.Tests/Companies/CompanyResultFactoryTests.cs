using LogisticsHub.CompanyService.Application.Companies.Results;
using LogisticsHub.CompanyService.Domain.Entities;
using LogisticsHub.CompanyService.Domain.Enums;
using Xunit;

namespace LogisticsHub.CompanyService.Tests.Companies;

public sealed class CompanyResultFactoryTests
{
    [Fact]
    public void ToResult_WhenCompanyIsProvided_ReturnsCompanyResult()
    {
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Acme",
            ExternalCode = "ACME",
            Status = CompanyStatus.Active,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow.AddMinutes(1)
        };

        var result = CompanyResultFactory.ToResult(company);

        Assert.Equal(company.Id, result.Id);
        Assert.Equal(company.Name, result.Name);
        Assert.Equal(company.ExternalCode, result.ExternalCode);
        Assert.Equal(company.Status, result.Status);
        Assert.Equal(company.CreatedAtUtc, result.CreatedAtUtc);
        Assert.Equal(company.UpdatedAtUtc, result.UpdatedAtUtc);
    }

    [Fact]
    public void ToResult_WhenAddressIsProvided_ReturnsCompanyAddressResult()
    {
        var address = new CompanyAddress
        {
            Id = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            AddressType = CompanyAddressType.Shipping,
            CountryCode = "US",
            City = "New York",
            PostalCode = "10001",
            Line1 = "1 Logistics Way",
            Line2 = "Suite 10",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow.AddMinutes(1)
        };

        var result = CompanyResultFactory.ToResult(address);

        Assert.Equal(address.Id, result.Id);
        Assert.Equal(address.CompanyId, result.CompanyId);
        Assert.Equal(address.AddressType, result.AddressType);
        Assert.Equal(address.CountryCode, result.CountryCode);
        Assert.Equal(address.City, result.City);
        Assert.Equal(address.PostalCode, result.PostalCode);
        Assert.Equal(address.Line1, result.Line1);
        Assert.Equal(address.Line2, result.Line2);
        Assert.Equal(address.CreatedAtUtc, result.CreatedAtUtc);
        Assert.Equal(address.UpdatedAtUtc, result.UpdatedAtUtc);
    }
}
