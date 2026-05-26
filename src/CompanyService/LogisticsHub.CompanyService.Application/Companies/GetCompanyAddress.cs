using LogisticsHub.CompanyService.Application.Persistence;
using LogisticsHub.Results;
using MediatR;

namespace LogisticsHub.CompanyService.Application.Companies;

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
        var cachedAddress = await _cache.GetAsync(query.CompanyId, query.AddressId, cancellationToken);
        if (cachedAddress is not null)
        {
            return Result<CompanyAddressResult>.Success(cachedAddress);
        }

        var address = await _dbContext.GetCompanyAddressAsync(
            query.CompanyId,
            query.AddressId,
            cancellationToken);

        if (address is null)
        {
            return Result<CompanyAddressResult>.Failure(CompanyErrors.AddressCompanyNotFound(query.CompanyId));
        }

        var result = CompanyResultFactory.ToResult(address);
        await _cache.SetAsync(result, cancellationToken);

        return Result<CompanyAddressResult>.Success(result);
    }
}
