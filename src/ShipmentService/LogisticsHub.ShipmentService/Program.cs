using LogisticsHub.ShipmentService.Application.Shipments;
using LogisticsHub.ShipmentService.Infrastructure.DependencyInjection;
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

// Register application services.
builder.Services.AddScoped<CreateShipment>();
builder.Services.AddScoped<GetShipment>();

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
