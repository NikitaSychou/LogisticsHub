using LogisticsHub.Caching;
using LogisticsHub.CompanyService.Application.Companies;
using LogisticsHub.CompanyService.Infrastructure.Caching;
using Microsoft.Extensions.Options;

namespace LogisticsHub.Workers.CacheWorker;

public sealed class CompanyAddressCacheWarmupModule : ICacheWarmupModule
{
    public const string ModuleName = "company-addresses";

    private readonly ICompanyCacheWarmupReader _reader;
    private readonly ICacheService _cache;
    private readonly ILogger<CompanyAddressCacheWarmupModule> _logger;
    private readonly CompanyAddressCacheWarmupOptions _options;

    public CompanyAddressCacheWarmupModule(
        ICompanyCacheWarmupReader reader,
        ICacheService cache,
        IOptions<CompanyAddressCacheWarmupOptions> options,
        ILogger<CompanyAddressCacheWarmupModule> logger)
    {
        _reader = reader;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public string Name => ModuleName;

    public async Task WarmUpAsync(CancellationToken cancellationToken)
    {
        var totalRead = 0;
        var totalCached = 0;
        var failedWrites = 0;
        var skip = 0;

        _logger.LogInformation("Company address cache warm-up started.");

        while (true)
        {
            var addresses = await _reader.ReadCompanyAddressesAsync(skip, _options.BatchSize, cancellationToken);
            if (addresses.Count == 0)
            {
                break;
            }

            var result = await CacheBatchAsync(addresses, cancellationToken);
            totalRead += addresses.Count;
            totalCached += result.Cached;
            failedWrites += result.Failed;
            skip += addresses.Count;

            _logger.LogInformation(
                "Company address cache warm-up processed {TotalRead} address record(s).",
                totalRead);

            if (addresses.Count < _options.BatchSize)
            {
                break;
            }
        }

        _logger.LogInformation(
            "Company address cache warm-up completed. Read: {TotalRead}. Cached: {TotalCached}. Failed writes: {FailedWrites}.",
            totalRead,
            totalCached,
            failedWrites);
    }

    private async Task<BatchCacheResult> CacheBatchAsync(
        IReadOnlyList<CompanyAddressResult> addresses,
        CancellationToken cancellationToken)
    {
        var cached = 0;
        var failed = 0;

        foreach (var address in addresses)
        {
            var key = RedisCompanyAddressCache.BuildKey(address.CompanyId, address.Id);
            var written = await _cache.SetAsync(key, address, cancellationToken: cancellationToken);
            if (written)
            {
                cached++;
            }
            else
            {
                failed++;
                _logger.LogWarning(
                    "Company address cache write failed for company {CompanyId}, address {AddressId}.",
                    address.CompanyId,
                    address.Id);
            }
        }

        return new BatchCacheResult(cached, failed);
    }

    private readonly record struct BatchCacheResult(int Cached, int Failed);
}
