namespace LogisticsHub.Workers.CacheWorker;

public interface ICacheWarmupModule
{
    string Name { get; }

    Task WarmUpAsync(CancellationToken cancellationToken);
}
