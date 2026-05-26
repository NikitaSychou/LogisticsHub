using LogisticsHub.AspNetCore;
using LogisticsHub.InventoryService.Application.InventoryItems;
using LogisticsHub.InventoryService.Consumers;
using LogisticsHub.InventoryService.Infrastructure.DependencyInjection;
using LogisticsHub.InventoryService.Outbox;
using LogisticsHub.Messaging.RabbitMQ;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using System.Text.Json.Serialization;

const string HealthEndpointPath = "/health";
const string LivenessHealthEndpointPath = "/health/live";
const string ReadinessHealthEndpointPath = "/health/ready";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbInfrastructure(builder.Configuration);
builder.Services.AddRabbitMqMessaging(builder.Configuration);
builder.Services
    .AddHealthChecks()
    .AddRabbitMqHealthCheck();
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
    configuration.RegisterServicesFromAssembly(typeof(CreateInventoryItem).Assembly);
});

builder.Services.AddHostedService<StockReservationRequestedConsumer>();
builder.Services.AddScoped<InventoryOutboxProcessor>();
builder.Services.AddHostedService<InventoryOutboxPublisherBackgroundService>();

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
        options.SwaggerEndpoint("/openapi/v1.json", "LogisticsHub Inventory API v1");
    });
}

app.MapHealthChecks(HealthEndpointPath);
app.MapHealthChecks(LivenessHealthEndpointPath, new HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks(ReadinessHealthEndpointPath);
app.MapControllers();

app.Run();
