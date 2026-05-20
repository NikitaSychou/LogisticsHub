using LogisticsHub.InventoryService.Application.InventoryItems;
using LogisticsHub.InventoryService.Application.StockReservations;
using LogisticsHub.InventoryService.Infrastructure.DependencyInjection;
using LogisticsHub.Messaging.RabbitMQ;
using System.Text.Json.Serialization;

const string HealthEndpointPath = "/health";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbInfrastructure(builder.Configuration);
builder.Services.AddRabbitMqMessaging(builder.Configuration);
builder.Services.AddHealthChecks();
builder.Services.AddOpenApi();
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddScoped<CreateInventoryItem>();
builder.Services.AddScoped<GetInventoryItem>();
builder.Services.AddScoped<CreateStockReservation>();
builder.Services.AddScoped<GetStockReservation>();

var app = builder.Build();

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
