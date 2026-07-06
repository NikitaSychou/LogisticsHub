using LogisticsHub.CompanyService.Domain.Enums;

namespace LogisticsHub.CompanyService.Application.Companies.Results;

public sealed record CompanyAddressResult(
    Guid Id,
    Guid CompanyId,
    CompanyAddressType AddressType,
    string CountryCode,
    string City,
    string? PostalCode,
    string Line1,
    string? Line2,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
