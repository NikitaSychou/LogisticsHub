using LogisticsHub.AspNetCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

const string ReverseProxySectionName = "ReverseProxy";
const string CorsPolicyName = "ConfiguredFrontendCors";
const string CorsAllowedOriginsSectionName = "Cors:AllowedOrigins";
const string HealthEndpointPath = "/health";
const string LivenessHealthEndpointPath = "/health/live";
const string ReadinessHealthEndpointPath = "/health/ready";

var builder = WebApplication.CreateBuilder(args);

var reverseProxySection = builder.Configuration.GetSection(ReverseProxySectionName);
var allowedCorsOrigins = GetAllowedCorsOrigins(builder.Configuration);

ValidateReverseProxyConfiguration(reverseProxySection);

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(reverseProxySection);

builder.Services.AddHealthChecks();
builder.Services.AddOpenApi(options => options.AddOpenApiSecurity(builder.Configuration));
builder.Services.AddApiAuthentication(builder.Configuration);
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        policy
            .WithOrigins(allowedCorsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCorrelationId();
app.UseApiExceptionHandling();
app.UseCors(CorsPolicyName);

app.UseApiAuthentication();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "LogisticsHub Gateway API v1");
        options.ConfigureOAuth(app.Configuration);
    });
}

app.MapHealthChecks(HealthEndpointPath);
app.MapHealthChecks(LivenessHealthEndpointPath, new HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks(ReadinessHealthEndpointPath);
app.MapReverseProxy()
    .RequireApiAuthentication();

app.Run();

static string[] GetAllowedCorsOrigins(IConfiguration configuration)
{
    var origins = configuration
        .GetSection(CorsAllowedOriginsSectionName)
        .Get<string[]>() ?? [];

    var configuredOrigins = origins
        .Select(origin => origin.Trim())
        .Where(origin => !string.IsNullOrWhiteSpace(origin))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    if (configuredOrigins.Length == 0)
    {
        throw new InvalidOperationException("Cors:AllowedOrigins must contain at least one allowed frontend origin.");
    }

    foreach (var origin in configuredOrigins)
    {
        if (origin.Contains('*', StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Cors:AllowedOrigins contains wildcard origin '{origin}'. Configure exact origins only.");
        }

        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            string.IsNullOrWhiteSpace(uri.Host) ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment) ||
            uri.AbsolutePath != "/" ||
            !string.Equals(origin, uri.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Cors:AllowedOrigins contains invalid origin '{origin}'. Use an exact HTTP or HTTPS origin without a path, query, fragment, or trailing slash.");
        }
    }

    return configuredOrigins;
}

static void ValidateReverseProxyConfiguration(IConfigurationSection reverseProxySection)
{
    if (!reverseProxySection.Exists())
    {
        throw new InvalidOperationException("Reverse proxy configuration section 'ReverseProxy' is missing.");
    }

    var routesSection = reverseProxySection.GetSection("Routes");
    var clustersSection = reverseProxySection.GetSection("Clusters");

    if (!routesSection.GetChildren().Any())
    {
        throw new InvalidOperationException("Reverse proxy configuration section 'ReverseProxy:Routes' must contain at least one route.");
    }

    if (!clustersSection.GetChildren().Any())
    {
        throw new InvalidOperationException("Reverse proxy configuration section 'ReverseProxy:Clusters' must contain at least one cluster.");
    }

    var clusterIds = clustersSection
        .GetChildren()
        .Select(cluster => cluster.Key)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    foreach (var routeSection in routesSection.GetChildren())
    {
        ValidateRoute(routeSection, clusterIds);
    }

    foreach (var clusterSection in clustersSection.GetChildren())
    {
        ValidateCluster(clusterSection);
    }
}

static void ValidateRoute(
    IConfigurationSection routeSection,
    ISet<string> clusterIds)
{
    var clusterId = routeSection["ClusterId"];

    if (string.IsNullOrWhiteSpace(clusterId))
    {
        throw new InvalidOperationException($"Reverse proxy route '{routeSection.Key}' must configure 'ClusterId'.");
    }

    if (!clusterIds.Contains(clusterId))
    {
        throw new InvalidOperationException($"Reverse proxy route '{routeSection.Key}' references unknown cluster '{clusterId}'.");
    }

    if (string.IsNullOrWhiteSpace(routeSection.GetSection("Match")["Path"]))
    {
        throw new InvalidOperationException($"Reverse proxy route '{routeSection.Key}' must configure 'Match:Path'.");
    }
}

static void ValidateCluster(IConfigurationSection clusterSection)
{
    var destinations = clusterSection.GetSection("Destinations").GetChildren().ToArray();

    if (destinations.Length == 0)
    {
        throw new InvalidOperationException($"Reverse proxy cluster '{clusterSection.Key}' must contain at least one destination.");
    }

    foreach (var destinationSection in destinations)
    {
        var address = destinationSection["Address"];

        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                $"Reverse proxy destination '{clusterSection.Key}:{destinationSection.Key}' must configure an absolute http or https 'Address'.");
        }
    }
}
