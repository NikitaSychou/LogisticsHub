using LogisticsHub.AspNetCore;

const string HealthEndpointPath = "/health";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
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
