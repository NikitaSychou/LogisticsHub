using CompanyEntity = LogisticsHub.CompanyService.Domain.Entities.Company;
using CompanyAddressEntity = LogisticsHub.CompanyService.Domain.Entities.CompanyAddress;

namespace LogisticsHub.CompanyService.Application.Companies.Results;

public static class CompanyResultFactory
{
    public static CompanyResult ToResult(CompanyEntity company)
    {
        return new CompanyResult(
            company.Id,
            company.Name,
            company.ExternalCode,
            company.Status,
            company.CreatedAtUtc,
            company.UpdatedAtUtc);
    }

    public static CompanyAddressResult ToResult(CompanyAddressEntity address)
    {
        return new CompanyAddressResult(
            address.Id,
            address.CompanyId,
            address.AddressType,
            address.CountryCode,
            address.City,
            address.PostalCode,
            address.Line1,
            address.Line2,
            address.CreatedAtUtc,
            address.UpdatedAtUtc);
    }
}
