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
        var consecutiveFailures = 0;
        var skip = 0;

        _logger.LogInformation("Company address cache warm-up started.");

        try
        {
            while (true)
            {
                var addresses = await _reader.ReadCompanyAddressesAsync(skip, _options.BatchSize, cancellationToken);
                if (addresses.Count == 0)
                {
                    break;
                }

                totalRead += addresses.Count;
                var result = await CacheBatchAsync(addresses, consecutiveFailures, cancellationToken);
                totalCached += result.Cached;
                failedWrites += result.Failed;
                consecutiveFailures = result.ConsecutiveFailures;
                skip += addresses.Count;

                _logger.LogInformation(
                    "Company address cache warm-up processed {TotalRead} address record(s).",
                    totalRead);

                if (result.ShouldStop)
                {
                    LogFailureThresholdReached(totalRead, totalCached, failedWrites);
                    return;
                }

                if (addresses.Count < _options.BatchSize)
                {
                    break;
                }
            }

            _logger.LogInformation(
                "Company address cache warm-up completed successfully. Read: {TotalRead}. Cached: {TotalCached}. Failed writes: {FailedWrites}.",
                totalRead,
                totalCached,
                failedWrites);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation(
                "Company address cache warm-up cancelled. Read: {TotalRead}. Cached: {TotalCached}. Failed writes: {FailedWrites}.",
                totalRead,
                totalCached,
                failedWrites);
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Company address cache warm-up failed. Read: {TotalRead}. Cached: {TotalCached}. Failed writes: {FailedWrites}.",
                totalRead,
                totalCached,
                failedWrites);
            throw;
        }
    }

    private async Task<BatchCacheResult> CacheBatchAsync(
        IReadOnlyList<CompanyAddressResult> addresses,
        int consecutiveFailures,
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
                consecutiveFailures = 0;
            }
            else
            {
                failed++;
                consecutiveFailures++;
                _logger.LogWarning(
                    "Company address cache write failed for company {CompanyId}, address {AddressId}.",
                    address.CompanyId,
                    address.Id);

                if (consecutiveFailures >= _options.ConsecutiveCacheWriteFailureThreshold)
                {
                    return new BatchCacheResult(cached, failed, consecutiveFailures, ShouldStop: true);
                }
            }
        }

        return new BatchCacheResult(cached, failed, consecutiveFailures, ShouldStop: false);
    }

    private void LogFailureThresholdReached(
        int totalRead,
        int totalCached,
        int failedWrites)
    {
        _logger.LogError(
            "Cache warm-up module {ModuleName} stopped after reaching {Threshold} consecutive cache write failure(s). Read: {TotalRead}. Cached: {TotalCached}. Failed writes: {FailedWrites}.",
            ModuleName,
            _options.ConsecutiveCacheWriteFailureThreshold,
            totalRead,
            totalCached,
            failedWrites);
    }

    private readonly record struct BatchCacheResult(
        int Cached,
        int Failed,
        int ConsecutiveFailures,
        bool ShouldStop);
}
