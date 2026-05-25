namespace LogisticsHub.CompanyService.Contracts;

public sealed record CreateCompanyAddressRequest(
    string? AddressType,
    string? CountryCode,
    string? City,
    string? PostalCode,
    string? Line1,
    string? Line2);
