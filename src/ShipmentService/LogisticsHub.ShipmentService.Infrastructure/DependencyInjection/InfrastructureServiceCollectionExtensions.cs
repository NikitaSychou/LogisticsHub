using LogisticsHub.ShipmentService.Application.Persistence;
using LogisticsHub.ShipmentService.Application.Companies;
using LogisticsHub.ShipmentService.Infrastructure.Companies;
using LogisticsHub.ShipmentService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LogisticsHub.ShipmentService.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddDbInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ShipmentDb");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ShipmentDb connection string is not configured.");
        }

        services.AddDbContext<ShipmentDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        services.AddScoped<IShipmentDbContext>(serviceProvider => serviceProvider.GetRequiredService<ShipmentDbContext>());

        return services;
    }

    public static IServiceCollection AddCompanyServiceClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var baseUrl = configuration["CompanyService:BaseUrl"];

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException("CompanyService base URL is not configured.");
        }

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
        {
            throw new InvalidOperationException("CompanyService base URL is not a valid absolute URI.");
        }

        services.AddHttpClient<ICompanyAddressReferenceClient, CompanyServiceClient>(client =>
        {
            client.BaseAddress = baseUri;
        });

        return services;
    }
}
