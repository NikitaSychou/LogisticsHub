using LogisticsHub.CompanyService.Application.Caching;
using LogisticsHub.CompanyService.Application.Persistence;
using LogisticsHub.Results;
using MediatR;

namespace LogisticsHub.CompanyService.Application.Companies;

public sealed class GetCompany : IRequestHandler<GetCompanyQuery, Result<CompanyResult>>
{
    private readonly ICompanyDbContext _dbContext;
    private readonly ICompanyCache _cache;

    public GetCompany(
        ICompanyDbContext dbContext,
        ICompanyCache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public async Task<Result<CompanyResult>> Handle(
        GetCompanyQuery query,
        CancellationToken cancellationToken)
    {
        var company = await _cache.GetOrCreateAsync(
            query.Id,
            token => LoadCompanyFromDatabaseAsync(query, token),
            cancellationToken);

        if (company is null)
        {
            return Result<CompanyResult>.Failure(CompanyErrors.NotFound(query.Id));
        }

        return Result<CompanyResult>.Success(company);
    }

    private async Task<CompanyResult?> LoadCompanyFromDatabaseAsync(
        GetCompanyQuery query,
        CancellationToken cancellationToken)
    {
        var company = await _dbContext.GetCompanyByIdAsync(query.Id, cancellationToken);

        return company is null
            ? null
            : CompanyResultFactory.ToResult(company);
    }
}
