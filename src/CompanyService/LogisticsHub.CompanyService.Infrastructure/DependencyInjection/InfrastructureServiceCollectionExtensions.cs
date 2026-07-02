using LogisticsHub.Caching;
using LogisticsHub.CompanyService.Application.Caching;
using LogisticsHub.CompanyService.Application.Persistence;
using LogisticsHub.CompanyService.Infrastructure.Caching;
using LogisticsHub.CompanyService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LogisticsHub.CompanyService.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddDbInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("CompanyDb");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'CompanyDb' is not configured.");
        }

        services.AddDbContext<CompanyDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<ICompanyDbContext>(serviceProvider => serviceProvider.GetRequiredService<CompanyDbContext>());

        return services;
    }

    public static IServiceCollection AddRedisCacheInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Redis");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'Redis' is not configured.");
        }

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = connectionString;
        });

        services.AddLogisticsHubCaching();
        services.AddSingleton<ICompanyCache, RedisCompanyCache>();
        services.AddSingleton<ICompanyAddressCache, RedisCompanyAddressCache>();

        return services;
    }

    public static IHealthChecksBuilder AddCompanyDbHealthCheck(this IHealthChecksBuilder builder)
    {
        return builder.AddDbContextCheck<CompanyDbContext>("CompanyDb");
    }
}
