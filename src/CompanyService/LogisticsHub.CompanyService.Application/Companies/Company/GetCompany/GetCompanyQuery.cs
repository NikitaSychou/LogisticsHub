using LogisticsHub.CompanyService.Application.Companies.Results;
using LogisticsHub.Results;
using MediatR;

namespace LogisticsHub.CompanyService.Application.Companies.Company.GetCompany;

public sealed record GetCompanyQuery(Guid Id) : IRequest<Result<CompanyResult>>;
