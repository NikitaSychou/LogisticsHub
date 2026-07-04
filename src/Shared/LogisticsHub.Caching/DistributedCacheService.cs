using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace LogisticsHub.Caching;

public sealed class DistributedCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<DistributedCacheService> _logger;
    private readonly CacheOptions _options;
    private readonly ConcurrentDictionary<string, Lazy<Task<object?>>> _inFlightLoads = new();

    public DistributedCacheService(
        IDistributedCache cache,
        CacheOptions options,
        ILogger<DistributedCacheService> logger)
    {
        _cache = cache;
        _options = options;
        _logger = logger;
    }

    public async Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> sourceFactory,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(sourceFactory);

        var cachedValue = await TryGetAsync<T>(key, cancellationToken);
        if (cachedValue.Found)
        {
            return cachedValue.Value;
        }

        var load = _inFlightLoads.GetOrAdd(
            key,
            _ => CreateInFlightLoad(key, sourceFactory, ttl));

        return (T?)await load.Value.WaitAsync(cancellationToken);
    }

    public async Task RemoveAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            await _cache.RemoveAsync(key, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Cache remove failed for key {CacheKey}.", key);
        }
    }

    public async Task<bool> SetAsync<T>(
        string key,
        T? value,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (value is null)
        {
            return false;
        }

        return await TrySetAsync(key, value, ttl, cancellationToken);
    }

    private Lazy<Task<object?>> CreateInFlightLoad<T>(
        string key,
        Func<CancellationToken, Task<T?>> sourceFactory,
        TimeSpan? ttl)
        where T : class
    {
        Lazy<Task<object?>>? load = null;
        load = new Lazy<Task<object?>>(
            () => RunInFlightLoadAsync(key, sourceFactory, ttl, load!),
            LazyThreadSafetyMode.ExecutionAndPublication);

        return load;
    }

    private async Task<object?> RunInFlightLoadAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> sourceFactory,
        TimeSpan? ttl,
        Lazy<Task<object?>> load)
        where T : class
    {
        try
        {
            return await LoadAndCacheAsync(key, sourceFactory, ttl, CancellationToken.None);
        }
        finally
        {
            _inFlightLoads.TryRemove(new KeyValuePair<string, Lazy<Task<object?>>>(key, load));
        }
    }

    private async Task<object?> LoadAndCacheAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> sourceFactory,
        TimeSpan? ttl,
        CancellationToken cancellationToken)
        where T : class
    {
        var cachedValue = await TryGetAsync<T>(key, cancellationToken);
        if (cachedValue.Found)
        {
            return cachedValue.Value;
        }

        var value = await sourceFactory(cancellationToken);
        if (value is null)
        {
            return null;
        }

        await TrySetAsync(key, value, ttl, cancellationToken);

        return value;
    }

    private async Task<CacheReadResult<T>> TryGetAsync<T>(
        string key,
        CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            var json = await _cache.GetStringAsync(key, cancellationToken);
            if (string.IsNullOrWhiteSpace(json))
            {
                _logger.LogDebug("Cache miss for key {CacheKey}.", key);
                return CacheReadResult<T>.Miss();
            }

            T? value;
            try
            {
                value = JsonSerializer.Deserialize<T>(json, _options.JsonSerializerOptions);
                if (value is null)
                {
                    _logger.LogWarning("Cache deserialization returned null for key {CacheKey}.", key);
                    return CacheReadResult<T>.Miss();
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogWarning(exception, "Cache deserialization failed for key {CacheKey}.", key);
                return CacheReadResult<T>.Miss();
            }

            _logger.LogDebug("Cache hit for key {CacheKey}.", key);
            return CacheReadResult<T>.Hit(value);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Cache read failed for key {CacheKey}.", key);
            return CacheReadResult<T>.Miss();
        }
    }

    private async Task<bool> TrySetAsync<T>(
        string key,
        T value,
        TimeSpan? ttl,
        CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            var json = JsonSerializer.Serialize(value, _options.JsonSerializerOptions);
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = GetEffectiveTtl(ttl)
            };

            await _cache.SetStringAsync(key, json, cacheOptions, cancellationToken);
            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Cache write failed for key {CacheKey}.", key);
            return false;
        }
    }

    private TimeSpan GetEffectiveTtl(TimeSpan? ttl)
    {
        var baseTtl = ttl ?? _options.DefaultTtl;
        if (_options.TtlJitterPercentage == 0)
        {
            return baseTtl;
        }

        var maximumOffsetTicks = baseTtl.Ticks * (_options.TtlJitterPercentage / 100);
        var offsetTicks = (long)Math.Round((Random.Shared.NextDouble() * 2 - 1) * maximumOffsetTicks);
        var effectiveTicks = Math.Max(1, baseTtl.Ticks + offsetTicks);

        return TimeSpan.FromTicks(effectiveTicks);
    }

    private readonly record struct CacheReadResult<T>(bool Found, T? Value)
        where T : class
    {
        public static CacheReadResult<T> Hit(T value)
        {
            return new CacheReadResult<T>(true, value);
        }

        public static CacheReadResult<T> Miss()
        {
            return new CacheReadResult<T>(false, null);
        }
    }
}
