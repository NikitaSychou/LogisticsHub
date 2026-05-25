using LogisticsHub.AspNetCore;

const string ReverseProxySectionName = "ReverseProxy";
const string HealthEndpointPath = "/health";

var builder = WebApplication.CreateBuilder(args);

var reverseProxySection = builder.Configuration.GetSection(ReverseProxySectionName);

ValidateReverseProxyConfiguration(reverseProxySection);

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(reverseProxySection);

builder.Services.AddHealthChecks();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseCorrelationId();
app.UseApiExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "LogisticsHub Gateway API v1");
    });
}

app.MapHealthChecks(HealthEndpointPath);
app.MapReverseProxy();

app.Run();

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
