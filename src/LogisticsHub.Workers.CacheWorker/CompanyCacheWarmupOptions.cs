namespace LogisticsHub.Workers.CacheWorker;

public sealed class CompanyCacheWarmupOptions
{
    public int BatchSize { get; set; } = 500;

    public int ConsecutiveCacheWriteFailureThreshold { get; set; } = 10;
}
