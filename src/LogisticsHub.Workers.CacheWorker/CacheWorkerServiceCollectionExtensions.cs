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
        services.AddHostedService<CacheWorkerBackgroundService>();

        return services;
    }
}
