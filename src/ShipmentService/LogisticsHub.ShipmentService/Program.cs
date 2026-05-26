using LogisticsHub.AspNetCore;
using LogisticsHub.ShipmentService.Application.Shipments;
using LogisticsHub.ShipmentService.Consumers;
using LogisticsHub.ShipmentService.Infrastructure.DependencyInjection;
using LogisticsHub.Messaging.RabbitMQ;
using LogisticsHub.ShipmentService.Outbox;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using System.Text.Json.Serialization;

const string HealthEndpointPath = "/health";
const string LivenessHealthEndpointPath = "/health/live";
const string ReadinessHealthEndpointPath = "/health/ready";

var builder = WebApplication.CreateBuilder(args);

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

// Register infrastructure dependencies.
builder.Services.AddDbInfrastructure(builder.Configuration);
builder.Services.AddCompanyServiceClient(builder.Configuration);
builder.Services.AddRabbitMqMessaging(builder.Configuration);

// Register application services.
builder.Services.AddMediatR(configuration =>
{
    configuration.RegisterServicesFromAssembly(typeof(CreateShipment).Assembly);
});

builder.Services.AddHostedService<StockReservedConsumer>();
builder.Services.AddHostedService<StockReservationFailedConsumer>();
builder.Services.AddScoped<ShipmentOutboxProcessor>();
builder.Services.AddHostedService<ShipmentOutboxPublisherBackgroundService>();

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
        options.SwaggerEndpoint("/openapi/v1.json", "LogisticsHub Shipment API v1");
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
