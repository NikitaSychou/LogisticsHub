namespace LogisticsHub.Workers.CacheWorker;

public sealed class CompanyCacheWarmupOptions
{
    public int BatchSize { get; set; } = 500;
}
