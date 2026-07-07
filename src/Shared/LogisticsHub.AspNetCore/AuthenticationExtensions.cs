using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace LogisticsHub.AspNetCore;

public static class AuthenticationExtensions
{
    private const string BearerSchemeName = "Bearer";
    private const string OAuthSchemeName = "OAuth2";

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

    public static OpenApiOptions AddOpenApiSecurity(
        this OpenApiOptions options,
        IConfiguration configuration)
    {
        var azureAdOptions = AzureAdAuthenticationOptions.FromConfiguration(configuration);
        var swaggerOAuthOptions = SwaggerOAuthOptions.TryFromConfiguration(configuration, azureAdOptions);

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
            if (swaggerOAuthOptions is not null)
            {
                document.Components.SecuritySchemes[OAuthSchemeName] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    Description = "Microsoft Entra ID authorization code flow with PKCE.",
                    Flows = new OpenApiOAuthFlows
                    {
                        AuthorizationCode = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = swaggerOAuthOptions.AuthorizationUrl,
                            TokenUrl = swaggerOAuthOptions.TokenUrl,
                            Scopes = new Dictionary<string, string>
                            {
                                [swaggerOAuthOptions.Scope] = "Access LogisticsHub APIs"
                            }
                        }
                    }
                };
            }

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
                    operation.Security.Add(CreateSecurityRequirement(document, swaggerOAuthOptions));
                }
            }

            return Task.CompletedTask;
        });

        return options;
    }

    public static SwaggerUIOptions ConfigureOAuth(
        this SwaggerUIOptions options,
        IConfiguration configuration)
    {
        var azureAdOptions = AzureAdAuthenticationOptions.FromConfiguration(configuration);
        var swaggerOAuthOptions = SwaggerOAuthOptions.TryFromConfiguration(configuration, azureAdOptions);

        if (swaggerOAuthOptions is null)
        {
            return options;
        }

        options.OAuthClientId(swaggerOAuthOptions.ClientId);
        options.OAuthScopes(swaggerOAuthOptions.Scope);
        options.OAuthUsePkce();

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

    private static OpenApiSecurityRequirement CreateSecurityRequirement(
        OpenApiDocument document,
        SwaggerOAuthOptions? swaggerOAuthOptions)
    {
        if (swaggerOAuthOptions is null)
        {
            return new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(BearerSchemeName, document)] = []
            };
        }

        return new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(OAuthSchemeName, document)] = [swaggerOAuthOptions.Scope]
        };
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

    private sealed record SwaggerOAuthOptions(
        string ClientId,
        string Scope,
        Uri AuthorizationUrl,
        Uri TokenUrl)
    {
        public static SwaggerOAuthOptions? TryFromConfiguration(
            IConfiguration configuration,
            AzureAdAuthenticationOptions azureAdOptions)
        {
            var section = configuration.GetSection("SwaggerOAuth");
            var clientId = section["ClientId"];
            var scope = section["Scope"];

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(scope))
            {
                return null;
            }

            var tenantBaseUrl = $"{azureAdOptions.Instance.TrimEnd('/')}/{azureAdOptions.TenantId}";

            return new SwaggerOAuthOptions(
                clientId,
                scope,
                new Uri($"{tenantBaseUrl}/oauth2/v2.0/authorize"),
                new Uri($"{tenantBaseUrl}/oauth2/v2.0/token"));
        }
    }
}
