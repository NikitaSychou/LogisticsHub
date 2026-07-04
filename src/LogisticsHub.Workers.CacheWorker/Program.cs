using LogisticsHub.Workers.CacheWorker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddCacheWorker(builder.Configuration);

await builder.Build().RunAsync();
