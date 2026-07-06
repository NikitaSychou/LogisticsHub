using LogisticsHub.CompanyService.Application.Companies.Results;
using LogisticsHub.Results;
using MediatR;

namespace LogisticsHub.CompanyService.Application.Companies.Addresses.GetCompanyAddress;

public sealed record GetCompanyAddressQuery(
    Guid CompanyId,
    Guid AddressId) : IRequest<Result<CompanyAddressResult>>;
