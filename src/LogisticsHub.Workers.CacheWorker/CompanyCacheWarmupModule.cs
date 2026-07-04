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
        var skip = 0;

        _logger.LogInformation("Company cache warm-up started.");

        while (true)
        {
            var companies = await _reader.ReadCompaniesAsync(skip, _options.BatchSize, cancellationToken);
            if (companies.Count == 0)
            {
                break;
            }

            var result = await CacheBatchAsync(companies, cancellationToken);
            totalRead += companies.Count;
            totalCached += result.Cached;
            failedWrites += result.Failed;
            skip += companies.Count;

            _logger.LogInformation(
                "Company cache warm-up processed {TotalRead} company record(s).",
                totalRead);

            if (companies.Count < _options.BatchSize)
            {
                break;
            }
        }

        _logger.LogInformation(
            "Company cache warm-up completed. Read: {TotalRead}. Cached: {TotalCached}. Failed writes: {FailedWrites}.",
            totalRead,
            totalCached,
            failedWrites);
    }

    private async Task<BatchCacheResult> CacheBatchAsync(
        IReadOnlyList<CompanyResult> companies,
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
            }
            else
            {
                failed++;
                _logger.LogWarning("Company cache write failed for company {CompanyId}.", company.Id);
            }
        }

        return new BatchCacheResult(cached, failed);
    }

    private readonly record struct BatchCacheResult(int Cached, int Failed);
}
