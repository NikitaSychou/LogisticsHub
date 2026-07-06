using LogisticsHub.Results;
using MediatR;

namespace LogisticsHub.CompanyService.Application.Companies;

public sealed record ListCompanyAddressesQuery(Guid CompanyId) : IRequest<Result<IReadOnlyList<CompanyAddressResult>>>;
