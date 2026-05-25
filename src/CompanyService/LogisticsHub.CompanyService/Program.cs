using LogisticsHub.AspNetCore;
using LogisticsHub.CompanyService.Infrastructure.DependencyInjection;

const string HealthEndpointPath = "/health";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbInfrastructure(builder.Configuration);
builder.Services
    .AddHealthChecks()
    .AddCompanyDbHealthCheck();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseCorrelationId();
app.UseApiExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "LogisticsHub Company API v1");
    });
}

app.MapHealthChecks(HealthEndpointPath);

app.Run();
