using LogisticsHub.InventoryService.Infrastructure.DependencyInjection;

const string HealthEndpointPath = "/health";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks();
builder.Services.AddOpenApi();

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

app.Run();
