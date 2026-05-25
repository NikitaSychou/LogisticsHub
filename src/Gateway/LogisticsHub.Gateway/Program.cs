using LogisticsHub.AspNetCore;

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
