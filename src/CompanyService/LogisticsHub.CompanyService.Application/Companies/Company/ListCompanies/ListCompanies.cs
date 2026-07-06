using LogisticsHub.CompanyService.Application.Companies.Results;
using LogisticsHub.CompanyService.Application.Persistence;
using MediatR;

namespace LogisticsHub.CompanyService.Application.Companies.Company.ListCompanies;

public sealed class ListCompanies : IRequestHandler<ListCompaniesQuery, IReadOnlyList<CompanyResult>>
{
    private readonly ICompanyDbContext _dbContext;

    public ListCompanies(ICompanyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<CompanyResult>> Handle(
        ListCompaniesQuery query,
        CancellationToken cancellationToken)
    {
        var companies = await _dbContext.ListCompaniesAsync(cancellationToken);

        return companies
            .Select(CompanyResultFactory.ToResult)
            .ToArray();
    }
}
