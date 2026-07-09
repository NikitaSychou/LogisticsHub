using LogisticsHub.Http.Resilience;
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

    public static IHealthChecksBuilder AddShipmentDbHealthCheck(this IHealthChecksBuilder builder)
    {
        return builder.AddDbContextCheck<ShipmentDbContext>("ShipmentDb");
    }

    public static IServiceCollection AddCompanyServiceClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = GetCompanyServiceClientOptions(configuration);

        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            throw new InvalidOperationException("CompanyService base URL is not configured.");
        }

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUri))
        {
            throw new InvalidOperationException("CompanyService base URL is not a valid absolute URI.");
        }

        options.Resilience.Validate("CompanyService:Resilience");
        services.AddSingleton(options);
        services.AddHttpContextAccessor();
        services.AddTransient<ForwardUserBearerTokenHandler>();

        services.AddHttpClient<ICompanyAddressReferenceClient, CompanyServiceClient>(client =>
        {
            client.BaseAddress = baseUri;
            client.Timeout = options.Resilience.Timeout;
        })
        .AddHttpMessageHandler<ForwardUserBearerTokenHandler>()
        .AddOutboundHttpResilience(options.Resilience);

        return services;
    }

    private static CompanyServiceClientOptions GetCompanyServiceClientOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection(CompanyServiceClientOptions.SectionName);
        var resilienceSection = section.GetSection("Resilience");

        return new CompanyServiceClientOptions
        {
            BaseUrl = section["BaseUrl"],
            Resilience = new OutboundHttpClientResilienceOptions
            {
                TimeoutSeconds = GetInt(resilienceSection, "TimeoutSeconds", 3),
                RetryCount = GetInt(resilienceSection, "RetryCount", 1),
                RetryDelayMilliseconds = GetInt(resilienceSection, "RetryDelayMilliseconds", 150),
                CircuitBreakerFailureThreshold = GetInt(resilienceSection, "CircuitBreakerFailureThreshold", 3),
                CircuitBreakerDurationSeconds = GetInt(resilienceSection, "CircuitBreakerDurationSeconds", 5)
            }
        };
    }

    private static int GetInt(IConfiguration configuration, string key, int defaultValue)
    {
        return int.TryParse(configuration[key], out var value)
            ? value
            : defaultValue;
    }
}
