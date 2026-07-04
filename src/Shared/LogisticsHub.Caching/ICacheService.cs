namespace LogisticsHub.Caching;

public interface ICacheService
{
    Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> sourceFactory,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
        where T : class;

    Task<bool> SetAsync<T>(
        string key,
        T? value,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
        where T : class;

    Task RemoveAsync(
        string key,
        CancellationToken cancellationToken = default);
}
