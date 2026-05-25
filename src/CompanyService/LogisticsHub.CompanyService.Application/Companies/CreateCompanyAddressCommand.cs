using LogisticsHub.CompanyService.Domain.Enums;
using LogisticsHub.Results;
using MediatR;

namespace LogisticsHub.CompanyService.Application.Companies;

public sealed record CreateCompanyAddressCommand(
    Guid CompanyId,
    CompanyAddressType AddressType,
    string CountryCode,
    string City,
    string? PostalCode,
    string Line1,
    string? Line2) : IRequest<Result<CompanyAddressResult>>;
