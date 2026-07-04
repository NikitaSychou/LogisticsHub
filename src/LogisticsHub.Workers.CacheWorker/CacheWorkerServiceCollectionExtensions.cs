using LogisticsHub.Caching;
using LogisticsHub.CompanyService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LogisticsHub.Workers.CacheWorker;

public static class CacheWorkerServiceCollectionExtensions
{
    public static IServiceCollection AddCacheWorker(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<CacheWorkerOptions>()
            .Bind(configuration.GetSection("CacheWorker"))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<CacheWorkerOptions>, CacheWorkerOptionsValidator>();
        services
            .AddOptions<CompanyCacheWarmupOptions>()
            .Bind(configuration.GetSection("CompanyCacheWarmup"))
            .Validate(options => options.BatchSize > 0, "CompanyCacheWarmup:BatchSize must be greater than zero.")
            .ValidateOnStart();
        services
            .AddOptions<CompanyAddressCacheWarmupOptions>()
            .Bind(configuration.GetSection("CompanyAddressCacheWarmup"))
            .Validate(options => options.BatchSize > 0, "CompanyAddressCacheWarmup:BatchSize must be greater than zero.")
            .ValidateOnStart();

        var companyDbConnectionString = configuration.GetConnectionString("CompanyDb");
        if (string.IsNullOrWhiteSpace(companyDbConnectionString))
        {
            throw new InvalidOperationException("Connection string 'CompanyDb' is not configured.");
        }

        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (string.IsNullOrWhiteSpace(redisConnectionString))
        {
            throw new InvalidOperationException("Connection string 'Redis' is not configured.");
        }

        services.AddDbContextFactory<CompanyDbContext>(options =>
            options.UseSqlServer(companyDbConnectionString));
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
        });
        services.AddLogisticsHubCaching();
        services.AddSingleton<ICompanyCacheWarmupReader, EfCompanyCacheWarmupReader>();
        services.AddSingleton<ICacheWarmupModule, CompanyCacheWarmupModule>();
        services.AddSingleton<ICacheWarmupModule, CompanyAddressCacheWarmupModule>();
        services.AddHostedService<CacheWorkerBackgroundService>();

        return services;
    }
}
