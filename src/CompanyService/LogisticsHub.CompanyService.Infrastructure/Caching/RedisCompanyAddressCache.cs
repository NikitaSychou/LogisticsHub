using LogisticsHub.CompanyService.Application.Caching;
using LogisticsHub.CompanyService.Application.Companies;
using LogisticsHub.Caching;

namespace LogisticsHub.CompanyService.Infrastructure.Caching;

public sealed class RedisCompanyAddressCache : ICompanyAddressCache
{
    private readonly ICacheService _cache;

    public RedisCompanyAddressCache(ICacheService cache)
    {
        _cache = cache;
    }

    public Task<CompanyAddressResult?> GetOrCreateAsync(
        Guid companyId,
        Guid addressId,
        Func<CancellationToken, Task<CompanyAddressResult?>> sourceFactory,
        CancellationToken cancellationToken = default)
        => _cache.GetOrCreateAsync(BuildKey(companyId, addressId), sourceFactory, cancellationToken: cancellationToken);

    public static string BuildKey(Guid companyId, Guid addressId)
        => $"company-address:{companyId:D}:{addressId:D}";
}
