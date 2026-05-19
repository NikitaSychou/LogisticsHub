using LogisticsHub.InventoryService.Infrastructure.DependencyInjection;

const string HealthEndpointPath = "/health";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks(HealthEndpointPath);

app.Run();