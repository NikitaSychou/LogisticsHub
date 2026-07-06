using LogisticsHub.Caching;
using LogisticsHub.CompanyService.Application.Caching;
using LogisticsHub.CompanyService.Application.Companies.Results;

namespace LogisticsHub.CompanyService.Infrastructure.Caching;

public sealed class RedisCompanyCache : ICompanyCache
{
    private readonly ICacheService _cache;

    public RedisCompanyCache(ICacheService cache)
    {
        _cache = cache;
    }

    public Task<CompanyResult?> GetOrCreateAsync(
        Guid companyId,
        Func<CancellationToken, Task<CompanyResult?>> sourceFactory,
        CancellationToken cancellationToken = default)
        => _cache.GetOrCreateAsync(BuildKey(companyId), sourceFactory, cancellationToken: cancellationToken);

    public Task InvalidateAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
        => _cache.RemoveAsync(BuildKey(companyId), cancellationToken);

    public static string BuildKey(Guid companyId)
        => $"company:{companyId:D}";
}
