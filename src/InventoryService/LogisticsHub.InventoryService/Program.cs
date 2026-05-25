using LogisticsHub.AspNetCore;
using LogisticsHub.InventoryService.Application.InventoryItems;
using LogisticsHub.InventoryService.Consumers;
using LogisticsHub.InventoryService.Infrastructure.DependencyInjection;
using LogisticsHub.InventoryService.Outbox;
using LogisticsHub.Messaging.RabbitMQ;
using System.Text.Json.Serialization;

const string HealthEndpointPath = "/health";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbInfrastructure(builder.Configuration);
builder.Services.AddRabbitMqMessaging(builder.Configuration);
builder.Services
    .AddHealthChecks()
    .AddRabbitMqHealthCheck();
builder.Services.AddOpenApi();
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
builder.Services.AddHostedService<InventoryOutboxPublisherBackgroundService>();

var app = builder.Build();

app.UseCorrelationId();
app.UseApiExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "LogisticsHub Inventory API v1");
    });
}

app.MapHealthChecks(HealthEndpointPath);
app.MapControllers();

app.Run();
