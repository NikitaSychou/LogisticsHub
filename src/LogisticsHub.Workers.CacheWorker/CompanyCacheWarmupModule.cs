using LogisticsHub.Caching;
using LogisticsHub.CompanyService.Application.Companies;
using LogisticsHub.CompanyService.Infrastructure.Caching;
using Microsoft.Extensions.Options;

namespace LogisticsHub.Workers.CacheWorker;

public sealed class CompanyCacheWarmupModule : ICacheWarmupModule
{
    public const string ModuleName = "companies";

    private readonly ICompanyCacheWarmupReader _reader;
    private readonly ICacheService _cache;
    private readonly ILogger<CompanyCacheWarmupModule> _logger;
    private readonly CompanyCacheWarmupOptions _options;

    public CompanyCacheWarmupModule(
        ICompanyCacheWarmupReader reader,
        ICacheService cache,
        IOptions<CompanyCacheWarmupOptions> options,
        ILogger<CompanyCacheWarmupModule> logger)
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

        _logger.LogInformation("Company cache warm-up started.");

        try
        {
            while (true)
            {
                var companies = await _reader.ReadCompaniesAsync(skip, _options.BatchSize, cancellationToken);
                if (companies.Count == 0)
                {
                    break;
                }

                totalRead += companies.Count;
                var result = await CacheBatchAsync(companies, consecutiveFailures, cancellationToken);
                totalCached += result.Cached;
                failedWrites += result.Failed;
                consecutiveFailures = result.ConsecutiveFailures;
                skip += companies.Count;

                _logger.LogInformation(
                    "Company cache warm-up processed {TotalRead} company record(s).",
                    totalRead);

                if (result.ShouldStop)
                {
                    LogFailureThresholdReached(totalRead, totalCached, failedWrites);
                    return;
                }

                if (companies.Count < _options.BatchSize)
                {
                    break;
                }
            }

            _logger.LogInformation(
                "Company cache warm-up completed successfully. Read: {TotalRead}. Cached: {TotalCached}. Failed writes: {FailedWrites}.",
                totalRead,
                totalCached,
                failedWrites);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation(
                "Company cache warm-up cancelled. Read: {TotalRead}. Cached: {TotalCached}. Failed writes: {FailedWrites}.",
                totalRead,
                totalCached,
                failedWrites);
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Company cache warm-up failed. Read: {TotalRead}. Cached: {TotalCached}. Failed writes: {FailedWrites}.",
                totalRead,
                totalCached,
                failedWrites);
            throw;
        }
    }

    private async Task<BatchCacheResult> CacheBatchAsync(
        IReadOnlyList<CompanyResult> companies,
        int consecutiveFailures,
        CancellationToken cancellationToken)
    {
        var cached = 0;
        var failed = 0;

        foreach (var company in companies)
        {
            var key = RedisCompanyCache.BuildKey(company.Id);
            var written = await _cache.SetAsync(key, company, cancellationToken: cancellationToken);
            if (written)
            {
                cached++;
                consecutiveFailures = 0;
            }
            else
            {
                failed++;
                consecutiveFailures++;
                _logger.LogWarning("Company cache write failed for company {CompanyId}.", company.Id);

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
