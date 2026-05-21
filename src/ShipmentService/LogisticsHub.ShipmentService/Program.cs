using LogisticsHub.ShipmentService.Application.Shipments;
using LogisticsHub.ShipmentService.Consumers;
using LogisticsHub.ShipmentService.Infrastructure.DependencyInjection;
using LogisticsHub.Messaging.RabbitMQ;
using LogisticsHub.ShipmentService.Outbox;
using System.Text.Json.Serialization;

const string HealthEndpointPath = "/health";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddOpenApi();

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Register infrastructure dependencies.
builder.Services.AddDbInfrastructure(builder.Configuration);
builder.Services.AddRabbitMqMessaging(builder.Configuration);

// Register application services.
builder.Services.AddMediatR(configuration =>
{
    configuration.RegisterServicesFromAssembly(typeof(CreateShipment).Assembly);
});

builder.Services.AddHostedService<StockReservedConsumer>();
builder.Services.AddHostedService<StockReservationFailedConsumer>();
builder.Services.AddHostedService<ShipmentOutboxPublisherBackgroundService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "LogisticsHub Shipment API v1");
    });
}

app.MapHealthChecks(HealthEndpointPath);
app.MapControllers();

app.Run();
