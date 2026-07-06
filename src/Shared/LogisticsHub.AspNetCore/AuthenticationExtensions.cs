using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

namespace LogisticsHub.AspNetCore;

public static class AuthenticationExtensions
{
    private const string BearerSchemeName = "Bearer";

    public static IServiceCollection AddApiAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var azureAdOptions = AzureAdAuthenticationOptions.FromConfiguration(configuration);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = azureAdOptions.Authority;
                options.Audience = azureAdOptions.Audience;
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidAudience = azureAdOptions.Audience
                };
            });
        services.AddAuthorization();

        return services;
    }

    public static OpenApiOptions AddOpenApiBearerSecurity(this OpenApiOptions options)
    {
        options.AddDocumentTransformer((document, _, _) =>
        {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes[BearerSchemeName] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Microsoft Entra ID JWT bearer token."
            };

            if (document.Paths is null)
            {
                return Task.CompletedTask;
            }

            foreach (var (path, pathItem) in document.Paths)
            {
                if (path.StartsWith("/health", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (pathItem.Operations is null)
                {
                    continue;
                }

                foreach (var operation in pathItem.Operations.Values)
                {
                    operation.Security ??= [];
                    operation.Security.Add(new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference(BearerSchemeName, document)] = []
                    });
                }
            }

            return Task.CompletedTask;
        });

        return options;
    }

    public static WebApplication UseApiAuthentication(this WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }

    public static TBuilder RequireApiAuthentication<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        return builder.RequireAuthorization();
    }

    private sealed record AzureAdAuthenticationOptions(
        string Instance,
        string TenantId,
        string ClientId,
        string Audience)
    {
        public string Authority => $"{Instance.TrimEnd('/')}/{TenantId}/v2.0";

        public static AzureAdAuthenticationOptions FromConfiguration(IConfiguration configuration)
        {
            var section = configuration.GetSection("AzureAd");
            var instance = Required(section, "Instance");
            var tenantId = Required(section, "TenantId");
            var clientId = Required(section, "ClientId");

            return new AzureAdAuthenticationOptions(
                instance,
                tenantId,
                clientId,
                Required(section, "Audience"));
        }

        private static string Required(IConfiguration section, string key)
        {
            var value = section[key];

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"AzureAd:{key} must be configured for API authentication.");
            }

            return value;
        }
    }
}
