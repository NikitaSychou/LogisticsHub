using LogisticsHub.Results;
using MediatR;

namespace LogisticsHub.CompanyService.Application.Companies;

public sealed record GetCompanyQuery(Guid Id) : IRequest<Result<CompanyResult>>;
