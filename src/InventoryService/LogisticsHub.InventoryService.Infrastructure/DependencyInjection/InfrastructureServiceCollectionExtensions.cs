using LogisticsHub.InventoryService.Application.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using LogisticsHub.InventoryService.Infrastructure.Persistence;

namespace LogisticsHub.InventoryService.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddDbInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("InventoryDb");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'InventoryDb' is not configured.");
        }

        services.AddDbContext<InventoryDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IInventoryDbContext>(serviceProvider => serviceProvider.GetRequiredService<InventoryDbContext>());

        return services;
    }

    public static IHealthChecksBuilder AddInventoryDbHealthCheck(this IHealthChecksBuilder builder)
    {
        return builder.AddDbContextCheck<InventoryDbContext>("InventoryDb");
    }
}
