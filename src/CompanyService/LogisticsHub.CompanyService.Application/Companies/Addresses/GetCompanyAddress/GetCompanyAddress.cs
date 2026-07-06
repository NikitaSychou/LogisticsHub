using LogisticsHub.CompanyService.Application.Companies;
using LogisticsHub.CompanyService.Application.Companies.Results;
using LogisticsHub.CompanyService.Application.Caching;
using LogisticsHub.CompanyService.Application.Persistence;
using LogisticsHub.Results;
using MediatR;

namespace LogisticsHub.CompanyService.Application.Companies.Addresses.GetCompanyAddress;

public sealed class GetCompanyAddress : IRequestHandler<GetCompanyAddressQuery, Result<CompanyAddressResult>>
{
    private readonly ICompanyDbContext _dbContext;
    private readonly ICompanyAddressCache _cache;

    public GetCompanyAddress(
        ICompanyDbContext dbContext,
        ICompanyAddressCache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public async Task<Result<CompanyAddressResult>> Handle(
        GetCompanyAddressQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _cache.GetOrCreateAsync(
            query.CompanyId,
            query.AddressId,
            token => LoadAddressFromDatabaseAsync(query, token),
            cancellationToken);

        if (result is null)
        {
            return Result<CompanyAddressResult>.Failure(CompanyErrors.AddressCompanyNotFound(query.CompanyId));
        }

        return Result<CompanyAddressResult>.Success(result);
    }

    private async Task<CompanyAddressResult?> LoadAddressFromDatabaseAsync(
        GetCompanyAddressQuery query,
        CancellationToken cancellationToken)
    {
        var address = await _dbContext.GetCompanyAddressAsync(
            query.CompanyId,
            query.AddressId,
            cancellationToken);

        return address is null
            ? null
            : CompanyResultFactory.ToResult(address);
    }
}
