using LogisticsHub.AspNetCore;
using LogisticsHub.CompanyService.Application.Companies;
using LogisticsHub.CompanyService.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using System.Text.Json.Serialization;

const string HealthEndpointPath = "/health";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbInfrastructure(builder.Configuration);
builder.Services.AddRedisCacheInfrastructure(builder.Configuration);
builder.Services
    .AddHealthChecks()
    .AddCompanyDbHealthCheck();
builder.Services.AddOpenApi();
builder.Services.AddLocalization(options =>
{
    options.ResourcesPath = "Resources";
});
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddMediatR(configuration =>
{
    configuration.RegisterServicesFromAssembly(typeof(CreateCompany).Assembly);
});

var app = builder.Build();

app.UseCorrelationId();
app.UseApiExceptionHandling();
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en"),
    SupportedCultures = [new CultureInfo("en"), new CultureInfo("uk")],
    SupportedUICultures = [new CultureInfo("en"), new CultureInfo("uk")]
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "LogisticsHub Company API v1");
    });
}

app.MapHealthChecks(HealthEndpointPath);
app.MapControllers();

app.Run();
