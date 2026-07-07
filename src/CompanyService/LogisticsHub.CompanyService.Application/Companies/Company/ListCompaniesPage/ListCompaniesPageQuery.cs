using LogisticsHub.CompanyService.Application.Companies.Results;
using LogisticsHub.Results;
using MediatR;

namespace LogisticsHub.CompanyService.Application.Companies.Company.ListCompaniesPage;

public sealed record ListCompaniesPageQuery(
    int PageNumber,
    int PageSize) : IRequest<PagedResponse<CompanyResult>>;
