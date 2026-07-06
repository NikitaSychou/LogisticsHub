using MediatR;

namespace LogisticsHub.CompanyService.Application.Companies;

public sealed record ListCompaniesQuery : IRequest<IReadOnlyList<CompanyResult>>;
