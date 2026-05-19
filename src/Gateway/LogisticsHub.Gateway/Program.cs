const string ReverseProxySectionName = "ReverseProxy";
const string HealthEndpointPath = "/health";

var builder = WebApplication.CreateBuilder(args);

var reverseProxySection = builder.Configuration.GetSection(ReverseProxySectionName);

if (!reverseProxySection.Exists())
{
    throw new InvalidOperationException($"{ReverseProxySectionName} configuration section is missing.");
}

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(reverseProxySection);

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks(HealthEndpointPath);
app.MapReverseProxy();

app.Run();