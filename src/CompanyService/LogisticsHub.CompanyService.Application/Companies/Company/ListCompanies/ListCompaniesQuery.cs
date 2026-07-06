using LogisticsHub.CompanyService.Application.Companies.Results;
using MediatR;

namespace LogisticsHub.CompanyService.Application.Companies.Company.ListCompanies;

public sealed record ListCompaniesQuery : IRequest<IReadOnlyList<CompanyResult>>;
