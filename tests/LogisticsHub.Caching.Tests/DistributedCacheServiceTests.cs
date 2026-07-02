using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using LogisticsHub.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LogisticsHub.Caching.Tests;

public sealed class DistributedCacheServiceTests
{
    [Fact]
    public async Task GetOrCreateAsync_WhenCacheHit_ReturnsCachedValueWithoutCallingSource()
    {
        var distributedCache = new FakeDistributedCache();
        var cachedValue = new CacheItem("cached");
        await distributedCache.SetStringAsync("item:1", JsonSerializer.Serialize(cachedValue, JsonOptions()));
        var cache = CreateCache(distributedCache);
        var sourceCalled = false;

        var result = await cache.GetOrCreateAsync(
            "item:1",
            _ =>
            {
                sourceCalled = true;
                return Task.FromResult<CacheItem?>(new CacheItem("source"));
            });

        Assert.NotNull(result);
        Assert.Equal("cached", result.Value);
        Assert.False(sourceCalled);
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenCacheMiss_CallsSourceAndStoresResult()
    {
        var distributedCache = new FakeDistributedCache();
        var cache = CreateCache(distributedCache);

        var result = await cache.GetOrCreateAsync(
            "item:1",
            _ => Task.FromResult<CacheItem?>(new CacheItem("source")));

        Assert.NotNull(result);
        Assert.Equal("source", result.Value);
        Assert.Equal(1, distributedCache.SetCallCount);
        Assert.NotNull(await distributedCache.GetStringAsync("item:1"));
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenConcurrentMissesForSameKey_CallsSourceOnce()
    {
        var distributedCache = new FakeDistributedCache();
        var cache = CreateCache(distributedCache);
        var releaseSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var sourceCalls = 0;

        var tasks = Enumerable.Range(0, 8)
            .Select(_ => cache.GetOrCreateAsync(
                "item:1",
                async _ =>
                {
                    Interlocked.Increment(ref sourceCalls);
                    await releaseSource.Task;
                    return new CacheItem("source");
                }))
            .ToArray();

        Assert.True(SpinWait.SpinUntil(() => Volatile.Read(ref sourceCalls) == 1, TimeSpan.FromSeconds(2)));
        releaseSource.SetResult();
        var results = await Task.WhenAll(tasks);

        Assert.Equal(1, sourceCalls);
        Assert.All(results, result => Assert.Equal("source", result?.Value));
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenFirstSameKeyCallerIsCancelled_SecondCallerReceivesSharedResult()
    {
        var distributedCache = new FakeDistributedCache();
        var cache = CreateCache(distributedCache);
        var releaseSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var sourceStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstCancellation = new CancellationTokenSource();
        var sourceCalls = 0;

        var first = cache.GetOrCreateAsync(
            "item:1",
            async token =>
            {
                Assert.False(token.CanBeCanceled);
                Interlocked.Increment(ref sourceCalls);
                sourceStarted.SetResult();
                await releaseSource.Task;
                return new CacheItem("source");
            },
            cancellationToken: firstCancellation.Token);

        await sourceStarted.Task;
        await firstCancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);

        var second = cache.GetOrCreateAsync<CacheItem>(
            "item:1",
            _ => throw new InvalidOperationException("source should not run twice"));

        releaseSource.SetResult();

        var result = await second;
        Assert.NotNull(result);
        Assert.Equal("source", result.Value);
        Assert.Equal(1, sourceCalls);
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenNonCreatorSameKeyCallerIsCancelled_OnlyCancelsThatWait()
    {
        var distributedCache = new FakeDistributedCache();
        var cache = CreateCache(distributedCache);
        var releaseSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var sourceStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondCancellation = new CancellationTokenSource();
        var sourceCalls = 0;

        var first = cache.GetOrCreateAsync(
            "item:1",
            async _ =>
            {
                Interlocked.Increment(ref sourceCalls);
                sourceStarted.SetResult();
                await releaseSource.Task;
                return new CacheItem("source");
            });

        await sourceStarted.Task;

        var second = cache.GetOrCreateAsync<CacheItem>(
            "item:1",
            _ => throw new InvalidOperationException("source should not run twice"),
            cancellationToken: secondCancellation.Token);

        await secondCancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second);

        releaseSource.SetResult();
        var result = await first;

        Assert.NotNull(result);
        Assert.Equal("source", result.Value);
        Assert.Equal(1, sourceCalls);
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenOneOfSeveralSameKeyCallersIsCancelled_DoesNotStartSecondSourceLoad()
    {
        var distributedCache = new FakeDistributedCache();
        var cache = CreateCache(distributedCache);
        var releaseSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var sourceStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondCancellation = new CancellationTokenSource();
        var sourceCalls = 0;

        var first = cache.GetOrCreateAsync(
            "item:1",
            async _ =>
            {
                Interlocked.Increment(ref sourceCalls);
                sourceStarted.SetResult();
                await releaseSource.Task;
                return new CacheItem("source");
            });

        await sourceStarted.Task;

        var second = cache.GetOrCreateAsync<CacheItem>(
            "item:1",
            _ => throw new InvalidOperationException("source should not run twice"),
            cancellationToken: secondCancellation.Token);
        var third = cache.GetOrCreateAsync<CacheItem>(
            "item:1",
            _ => throw new InvalidOperationException("source should not run twice"));

        await secondCancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second);

        releaseSource.SetResult();

        Assert.Equal("source", (await first)?.Value);
        Assert.Equal("source", (await third)?.Value);
        Assert.Equal(1, sourceCalls);
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenAllOriginalWaitersCancel_CleansUpSuccessfulInFlightLoad()
    {
        var setStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseSet = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var distributedCache = new FakeDistributedCache
        {
            FailOnSet = true,
            SetStarted = setStarted,
            ReleaseSet = releaseSet
        };
        var cache = CreateCache(distributedCache);
        var releaseSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstCancellation = new CancellationTokenSource();
        var secondCancellation = new CancellationTokenSource();
        var sourceCalls = 0;

        var first = cache.GetOrCreateAsync(
            "item:1",
            async _ =>
            {
                Interlocked.Increment(ref sourceCalls);
                await releaseSource.Task;
                return new CacheItem("first");
            },
            cancellationToken: firstCancellation.Token);
        var second = cache.GetOrCreateAsync<CacheItem>(
            "item:1",
            _ => throw new InvalidOperationException("source should not run twice"),
            cancellationToken: secondCancellation.Token);

        await firstCancellation.CancelAsync();
        await secondCancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second);

        releaseSource.SetResult();
        await setStarted.Task;

        var observer = cache.GetOrCreateAsync<CacheItem>(
            "item:1",
            _ => throw new InvalidOperationException("source should not run twice"));

        releaseSet.SetResult();
        Assert.Equal("first", (await observer)?.Value);

        var result = await cache.GetOrCreateAsync(
            "item:1",
            _ =>
            {
                Interlocked.Increment(ref sourceCalls);
                return Task.FromResult<CacheItem?>(new CacheItem("second"));
            });

        Assert.NotNull(result);
        Assert.Equal("second", result.Value);
        Assert.Equal(2, sourceCalls);
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenDifferentKeysMiss_DoesNotBlockOnOneGlobalLock()
    {
        var distributedCache = new FakeDistributedCache();
        var cache = CreateCache(distributedCache);
        var releaseFirstSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstSourceStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var first = cache.GetOrCreateAsync(
            "item:1",
            async _ =>
            {
                firstSourceStarted.SetResult();
                await releaseFirstSource.Task;
                return new CacheItem("first");
            });

        await firstSourceStarted.Task;

        var second = cache.GetOrCreateAsync(
            "item:2",
            _ => Task.FromResult<CacheItem?>(new CacheItem("second")));

        var completed = await Task.WhenAny(second, Task.Delay(TimeSpan.FromSeconds(2)));
        releaseFirstSource.SetResult();
        await first;

        Assert.Same(second, completed);
        Assert.Equal("second", (await second)?.Value);
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenCacheReadFails_FallsBackToSource()
    {
        var distributedCache = new FakeDistributedCache { FailOnGet = true };
        var cache = CreateCache(distributedCache);

        var result = await cache.GetOrCreateAsync(
            "item:1",
            _ => Task.FromResult<CacheItem?>(new CacheItem("source")));

        Assert.NotNull(result);
        Assert.Equal("source", result.Value);
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenCachedJsonIsInvalid_FallsBackToSource()
    {
        var distributedCache = new FakeDistributedCache();
        await distributedCache.SetStringAsync("item:1", "{");
        var cache = CreateCache(distributedCache);

        var result = await cache.GetOrCreateAsync(
            "item:1",
            _ => Task.FromResult<CacheItem?>(new CacheItem("source")));

        Assert.NotNull(result);
        Assert.Equal("source", result.Value);
        Assert.Equal(2, distributedCache.SetCallCount);
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenCacheWriteFails_ReturnsSourceValue()
    {
        var distributedCache = new FakeDistributedCache { FailOnSet = true };
        var cache = CreateCache(distributedCache);

        var result = await cache.GetOrCreateAsync(
            "item:1",
            _ => Task.FromResult<CacheItem?>(new CacheItem("source")));

        Assert.NotNull(result);
        Assert.Equal("source", result.Value);
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenSourceReturnsNull_DoesNotCacheResult()
    {
        var distributedCache = new FakeDistributedCache();
        var cache = CreateCache(distributedCache);

        var result = await cache.GetOrCreateAsync<CacheItem>(
            "item:1",
            _ => Task.FromResult<CacheItem?>(null));

        Assert.Null(result);
        Assert.Equal(0, distributedCache.SetCallCount);
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenSourceReturnsNull_CleansUpInFlightLoad()
    {
        var distributedCache = new FakeDistributedCache();
        var cache = CreateCache(distributedCache);
        var sourceCalls = 0;

        var first = await cache.GetOrCreateAsync<CacheItem>(
            "item:1",
            _ =>
            {
                Interlocked.Increment(ref sourceCalls);
                return Task.FromResult<CacheItem?>(null);
            });
        var second = await cache.GetOrCreateAsync(
            "item:1",
            _ =>
            {
                Interlocked.Increment(ref sourceCalls);
                return Task.FromResult<CacheItem?>(new CacheItem("source"));
            });

        Assert.Null(first);
        Assert.NotNull(second);
        Assert.Equal("source", second.Value);
        Assert.Equal(2, sourceCalls);
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenSourceThrows_PropagatesException()
    {
        var distributedCache = new FakeDistributedCache();
        var cache = CreateCache(distributedCache);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            cache.GetOrCreateAsync<CacheItem>(
                "item:1",
                _ => throw new InvalidOperationException("source failed")));

        Assert.Equal("source failed", exception.Message);
        Assert.Equal(0, distributedCache.SetCallCount);
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenSourceThrows_PropagatesExceptionToActiveWaiters()
    {
        var distributedCache = new FakeDistributedCache();
        var cache = CreateCache(distributedCache);
        var releaseSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var sourceStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var sourceCalls = 0;

        var first = cache.GetOrCreateAsync<CacheItem>(
            "item:1",
            async _ =>
            {
                Interlocked.Increment(ref sourceCalls);
                sourceStarted.SetResult();
                await releaseSource.Task;
                throw new InvalidOperationException("source failed");
            });

        await sourceStarted.Task;

        var second = cache.GetOrCreateAsync<CacheItem>(
            "item:1",
            _ => throw new InvalidOperationException("source should not run twice"));

        releaseSource.SetResult();

        var firstException = await Assert.ThrowsAsync<InvalidOperationException>(() => first);
        var secondException = await Assert.ThrowsAsync<InvalidOperationException>(() => second);
        Assert.Equal("source failed", firstException.Message);
        Assert.Equal("source failed", secondException.Message);
        Assert.Equal(1, sourceCalls);
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenSourceThrows_CleansUpInFlightLoadAndAllowsRetry()
    {
        var distributedCache = new FakeDistributedCache();
        var cache = CreateCache(distributedCache);
        var sourceCalls = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            cache.GetOrCreateAsync<CacheItem>(
                "item:1",
                _ =>
                {
                    Interlocked.Increment(ref sourceCalls);
                    throw new InvalidOperationException("source failed");
                }));

        var result = await cache.GetOrCreateAsync(
            "item:1",
            _ =>
            {
                Interlocked.Increment(ref sourceCalls);
                return Task.FromResult<CacheItem?>(new CacheItem("source"));
            });

        Assert.NotNull(result);
        Assert.Equal("source", result.Value);
        Assert.Equal(2, sourceCalls);
    }

    [Fact]
    public async Task RemoveAsync_WhenCacheRemoveFails_DoesNotPropagate()
    {
        var distributedCache = new FakeDistributedCache { FailOnRemove = true };
        var cache = CreateCache(distributedCache);

        await cache.RemoveAsync("item:1");

        Assert.Equal(1, distributedCache.RemoveCallCount);
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenNoTtlIsProvided_UsesDefaultTtl()
    {
        var distributedCache = new FakeDistributedCache();
        var cache = CreateCache(distributedCache);

        await cache.GetOrCreateAsync(
            "item:1",
            _ => Task.FromResult<CacheItem?>(new CacheItem("source")));

        Assert.Equal(TimeSpan.FromHours(24), distributedCache.LastOptions?.AbsoluteExpirationRelativeToNow);
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenTtlIsConfigured_UsesConfiguredTtl()
    {
        var distributedCache = new FakeDistributedCache();
        var cache = CreateCache(distributedCache, TimeSpan.FromMinutes(15));

        await cache.GetOrCreateAsync(
            "item:1",
            _ => Task.FromResult<CacheItem?>(new CacheItem("source")));

        Assert.Equal(TimeSpan.FromMinutes(15), distributedCache.LastOptions?.AbsoluteExpirationRelativeToNow);
    }

    private static DistributedCacheService CreateCache(
        FakeDistributedCache distributedCache,
        TimeSpan? defaultTtl = null)
    {
        var options = new CacheOptions();
        if (defaultTtl is not null)
        {
            options.DefaultTtl = defaultTtl.Value;
        }

        return new DistributedCacheService(
            distributedCache,
            options,
            NullLogger<DistributedCacheService>.Instance);
    }

    private static JsonSerializerOptions JsonOptions()
    {
        return new CacheOptions().JsonSerializerOptions;
    }

    private sealed record CacheItem(string Value);

    private sealed class FakeDistributedCache : IDistributedCache
    {
        private readonly ConcurrentDictionary<string, byte[]> _items = new();

        public bool FailOnGet { get; init; }

        public bool FailOnSet { get; init; }

        public bool FailOnRemove { get; init; }

        public int SetCallCount { get; private set; }

        public int RemoveCallCount { get; private set; }

        public DistributedCacheEntryOptions? LastOptions { get; private set; }

        public TaskCompletionSource? SetStarted { get; init; }

        public TaskCompletionSource? ReleaseSet { get; init; }

        public byte[]? Get(string key)
        {
            if (FailOnGet)
            {
                throw new InvalidOperationException("get failed");
            }

            return _items.TryGetValue(key, out var value) ? value : null;
        }

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        {
            return Task.FromResult(Get(key));
        }

        public void Refresh(string key)
        {
        }

        public Task RefreshAsync(string key, CancellationToken token = default)
        {
            return Task.CompletedTask;
        }

        public void Remove(string key)
        {
            RemoveCallCount++;
            if (FailOnRemove)
            {
                throw new InvalidOperationException("remove failed");
            }

            _items.TryRemove(key, out _);
        }

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            Remove(key);
            return Task.CompletedTask;
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            SetCallCount++;
            LastOptions = options;
            if (FailOnSet)
            {
                throw new InvalidOperationException("set failed");
            }

            _items[key] = value;
        }

        public async Task SetAsync(
            string key,
            byte[] value,
            DistributedCacheEntryOptions options,
            CancellationToken token = default)
        {
            SetCallCount++;
            LastOptions = options;
            SetStarted?.SetResult();

            if (ReleaseSet is not null)
            {
                await ReleaseSet.Task;
            }

            if (FailOnSet)
            {
                throw new InvalidOperationException("set failed");
            }

            _items[key] = value;
        }
    }
}
