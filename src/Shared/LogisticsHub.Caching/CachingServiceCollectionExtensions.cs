using Microsoft.Extensions.DependencyInjection;

namespace LogisticsHub.Caching;

public static class CachingServiceCollectionExtensions
{
    public static IServiceCollection AddLogisticsHubCaching(
        this IServiceCollection services,
        Action<CacheOptions>? configure = null)
    {
        var options = new CacheOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<ICacheService, DistributedCacheService>();

        return services;
    }
}
