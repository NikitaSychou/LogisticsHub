using LogisticsHub.CompanyService.Application.Companies.Results;
using LogisticsHub.Results;
using MediatR;

namespace LogisticsHub.CompanyService.Application.Companies.Addresses.ListCompanyAddresses;

public sealed record ListCompanyAddressesQuery(Guid CompanyId) : IRequest<Result<IReadOnlyList<CompanyAddressResult>>>;
