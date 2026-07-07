using LogisticsHub.CompanyService.Application.Companies.Results;
using LogisticsHub.CompanyService.Application.Persistence;
using LogisticsHub.Results;
using MediatR;

namespace LogisticsHub.CompanyService.Application.Companies.Company.ListCompaniesPage;

public sealed class ListCompaniesPage : IRequestHandler<ListCompaniesPageQuery, PagedResponse<CompanyResult>>
{
    private readonly ICompanyDbContext _dbContext;

    public ListCompaniesPage(ICompanyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResponse<CompanyResult>> Handle(
        ListCompaniesPageQuery query,
        CancellationToken cancellationToken)
    {
        var companies = await _dbContext.ListCompaniesPageAsync(
            query.PageNumber,
            query.PageSize,
            cancellationToken);
        var items = companies
            .Take(query.PageSize)
            .Select(CompanyResultFactory.ToResult)
            .ToArray();

        return new PagedResponse<CompanyResult>(
            items,
            query.PageNumber,
            query.PageSize,
            companies.Count > query.PageSize);
    }
}
