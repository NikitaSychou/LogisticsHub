namespace LogisticsHub.Workers.CacheWorker;

public sealed class CompanyAddressCacheWarmupOptions
{
    public int BatchSize { get; set; } = 500;
}
