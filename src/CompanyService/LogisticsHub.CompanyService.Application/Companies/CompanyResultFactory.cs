using LogisticsHub.CompanyService.Domain.Entities;

namespace LogisticsHub.CompanyService.Application.Companies;

internal static class CompanyResultFactory
{
    public static CompanyResult ToResult(Company company)
    {
        return new CompanyResult(
            company.Id,
            company.Name,
            company.ExternalCode,
            company.Status,
            company.CreatedAtUtc,
            company.UpdatedAtUtc);
    }

    public static CompanyAddressResult ToResult(CompanyAddress address)
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
