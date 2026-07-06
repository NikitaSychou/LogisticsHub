using LogisticsHub.Results;
using MediatR;

namespace LogisticsHub.CompanyService.Application.Companies.GetCompanyAddress;

public sealed record GetCompanyAddressQuery(
    Guid CompanyId,
    Guid AddressId) : IRequest<Result<CompanyAddressResult>>;
