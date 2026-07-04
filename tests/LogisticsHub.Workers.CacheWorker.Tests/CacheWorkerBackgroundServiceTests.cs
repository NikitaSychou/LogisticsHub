using LogisticsHub.Workers.CacheWorker;
using LogisticsHub.Caching;
using LogisticsHub.CompanyService.Application.Companies;
using LogisticsHub.CompanyService.Domain.Enums;
using LogisticsHub.CompanyService.Infrastructure.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace LogisticsHub.Workers.CacheWorker.Tests;

public sealed class CacheWorkerBackgroundServiceTests
{
    [Fact]
    public async Task RunWarmupAsync_WhenNoModulesAreRegistered_DoesNotFail()
    {
        var service = CreateService([]);

        await service.RunWarmupAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_WhenRunOnceIsEnabled_ExecutesOneRunAndStopsApplication()
    {
        var module = new FakeWarmupModule("companies");
        var applicationLifetime = new FakeHostApplicationLifetime();
        var service = CreateService(
            [module],
            applicationLifetime,
            new CacheWorkerOptions
            {
                RunOnce = true,
                StartupJitterPercentage = 0
            });

        await service.StartAsync(CancellationToken.None);
        await applicationLifetime.StopRequested.Task;
        await service.StopAsync(CancellationToken.None);

        Assert.Equal(1, module.CallCount);
        Assert.Equal(1, applicationLifetime.StopApplicationCallCount);
    }

    [Fact]
    public async Task RunWarmupAsync_WhenModuleIsDisabled_SkipsModule()
    {
        var module = new FakeWarmupModule("companies");
        var service = CreateService(
            [module],
            options: new CacheWorkerOptions
            {
                EnabledModules = ["inventory"]
            });

        await service.RunWarmupAsync(CancellationToken.None);

        Assert.Equal(0, module.CallCount);
    }

    [Fact]
    public async Task RunWarmupAsync_WhenModuleFails_RunsOtherModules()
    {
        var failingModule = new FakeWarmupModule("companies")
        {
            Exception = new InvalidOperationException("warm-up failed")
        };
        var successfulModule = new FakeWarmupModule("company-addresses");
        var service = CreateService(
            [failingModule, successfulModule],
            options: new CacheWorkerOptions
            {
                MaxDegreeOfParallelism = 1
            });

        await service.RunWarmupAsync(CancellationToken.None);

        Assert.Equal(1, failingModule.CallCount);
        Assert.Equal(1, successfulModule.CallCount);
    }

    [Fact]
    public async Task RunWarmupAsync_WhenCancellationIsRequested_PropagatesCancellation()
    {
        var releaseModule = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var module = new FakeWarmupModule("companies")
        {
            Release = releaseModule
        };
        var service = CreateService([module]);
        using var cancellation = new CancellationTokenSource();

        var run = service.RunWarmupAsync(cancellation.Token);
        await module.Started.Task;
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
        Assert.True(module.ObservedCancellation);
    }

    [Fact]
    public async Task RunWarmupAsync_WhenGlobalTimeoutExpires_DoesNotPropagateCancellation()
    {
        var module = new FakeWarmupModule("companies")
        {
            Release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)
        };
        var service = CreateService(
            [module],
            options: new CacheWorkerOptions
            {
                GlobalTimeout = TimeSpan.FromMilliseconds(10)
            });

        await service.RunWarmupAsync(CancellationToken.None);

        Assert.Equal(1, module.CallCount);
        Assert.True(module.ObservedCancellation);
    }

    [Fact]
    public async Task RunWarmupAsync_AfterGlobalTimeout_CanRunAgain()
    {
        var releaseModule = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var module = new FakeWarmupModule("companies")
        {
            Release = releaseModule
        };
        var service = CreateService(
            [module],
            options: new CacheWorkerOptions
            {
                GlobalTimeout = TimeSpan.FromMilliseconds(10)
            });

        await service.RunWarmupAsync(CancellationToken.None);
        releaseModule.SetResult();
        await service.RunWarmupAsync(CancellationToken.None);

        Assert.Equal(2, module.CallCount);
    }

    [Fact]
    public async Task StartAsync_WhenRunOnceStartupRunTimesOut_StopsApplication()
    {
        var module = new FakeWarmupModule("companies")
        {
            Release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)
        };
        var applicationLifetime = new FakeHostApplicationLifetime();
        var service = CreateService(
            [module],
            applicationLifetime,
            new CacheWorkerOptions
            {
                RunOnce = true,
                StartupJitterPercentage = 0,
                GlobalTimeout = TimeSpan.FromMilliseconds(10)
            });

        await service.StartAsync(CancellationToken.None);
        await applicationLifetime.StopRequested.Task;
        await service.StopAsync(CancellationToken.None);

        Assert.Equal(1, module.CallCount);
        Assert.Equal(1, applicationLifetime.StopApplicationCallCount);
    }

    [Fact]
    public async Task RunWarmupAsync_UsesConfiguredMaxDegreeOfParallelism()
    {
        var tracker = new ConcurrencyTracker();
        var releaseModules = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var modules = Enumerable.Range(0, 4)
            .Select(index => new TrackingWarmupModule($"module-{index}", tracker, releaseModules))
            .ToArray<ICacheWarmupModule>();
        var service = CreateService(
            modules,
            options: new CacheWorkerOptions
            {
                MaxDegreeOfParallelism = 2
            });

        var run = service.RunWarmupAsync(CancellationToken.None);
        await tracker.WaitForActiveCountAsync(2);

        Assert.Equal(2, tracker.MaxActiveCount);

        releaseModules.SetResult();
        await run;

        Assert.Equal(4, tracker.TotalStarted);
        Assert.Equal(2, tracker.MaxActiveCount);
    }

    [Theory]
    [MemberData(nameof(InvalidOptions))]
    public void CacheWorkerOptionsValidator_WhenOptionsAreInvalid_ReturnsFailure(CacheWorkerOptions options)
    {
        var validator = new CacheWorkerOptionsValidator();

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
    }

    [Fact]
    public async Task CompanyCacheWarmupModule_ReadsCompaniesInBatchesAndCachesExpectedKeys()
    {
        var companies = new[]
        {
            CreateCompany(Guid.Parse("00000000-0000-0000-0000-000000000001"), "first"),
            CreateCompany(Guid.Parse("00000000-0000-0000-0000-000000000002"), "second"),
            CreateCompany(Guid.Parse("00000000-0000-0000-0000-000000000003"), "third")
        };
        var reader = new FakeCompanyCacheWarmupReader(companies: companies);
        var cache = new FakeCacheService();
        var module = CreateCompanyModule(reader, cache, batchSize: 2);

        await module.WarmUpAsync(CancellationToken.None);

        Assert.Equal([0, 2], reader.CompanySkips);
        Assert.Equal(3, cache.SetCalls.Count);
        Assert.Equal(
            companies.Select(company => RedisCompanyCache.BuildKey(company.Id)),
            cache.SetCalls.Select(call => call.Key));
    }

    [Fact]
    public async Task CompanyAddressCacheWarmupModule_ReadsAddressesInBatchesAndCachesExpectedKeys()
    {
        var addresses = new[]
        {
            CreateAddress(Guid.Parse("00000000-0000-0000-0000-000000000001")),
            CreateAddress(Guid.Parse("00000000-0000-0000-0000-000000000002")),
            CreateAddress(Guid.Parse("00000000-0000-0000-0000-000000000003"))
        };
        var reader = new FakeCompanyCacheWarmupReader(addresses: addresses);
        var cache = new FakeCacheService();
        var module = CreateCompanyAddressModule(reader, cache, batchSize: 2);

        await module.WarmUpAsync(CancellationToken.None);

        Assert.Equal([0, 2], reader.AddressSkips);
        Assert.Equal(3, cache.SetCalls.Count);
        Assert.Equal(
            addresses.Select(address => RedisCompanyAddressCache.BuildKey(address.CompanyId, address.Id)),
            cache.SetCalls.Select(call => call.Key));
    }

    [Fact]
    public async Task CompanyCacheWarmupModule_WhenCacheWriteFails_ContinuesCachingRemainingCompanies()
    {
        var companies = new[]
        {
            CreateCompany("first"),
            CreateCompany("second")
        };
        var reader = new FakeCompanyCacheWarmupReader(companies: companies);
        var cache = new FakeCacheService
        {
            FailedKeys = [RedisCompanyCache.BuildKey(companies[0].Id)]
        };
        var module = CreateCompanyModule(reader, cache, batchSize: 2);

        await module.WarmUpAsync(CancellationToken.None);

        Assert.Equal(2, cache.SetCalls.Count);
    }

    [Fact]
    public async Task CompanyCacheWarmupModule_WhenConsecutiveCacheWriteFailureThresholdIsReached_StopsCurrentModule()
    {
        var companies = new[]
        {
            CreateCompany("first"),
            CreateCompany("second"),
            CreateCompany("third")
        };
        var reader = new FakeCompanyCacheWarmupReader(companies: companies);
        var logger = new TestLogger<CompanyCacheWarmupModule>();
        var cache = new FakeCacheService
        {
            FailedKeys =
            [
                RedisCompanyCache.BuildKey(companies[0].Id),
                RedisCompanyCache.BuildKey(companies[1].Id),
                RedisCompanyCache.BuildKey(companies[2].Id)
            ]
        };
        var module = CreateCompanyModule(reader, cache, batchSize: 3, failureThreshold: 2, logger);

        await module.WarmUpAsync(CancellationToken.None);

        Assert.Equal([0], reader.CompanySkips);
        Assert.Equal(2, cache.SetCalls.Count);
        Assert.Contains(logger.Messages, message => message.Contains("stopped after reaching", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Messages, message => message.Contains("completed successfully", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CompanyAddressCacheWarmupModule_WhenConsecutiveCacheWriteFailureThresholdIsReached_StopsCurrentModule()
    {
        var addresses = new[]
        {
            CreateAddress(),
            CreateAddress(),
            CreateAddress()
        };
        var reader = new FakeCompanyCacheWarmupReader(addresses: addresses);
        var cache = new FakeCacheService
        {
            FailedKeys =
            [
                RedisCompanyAddressCache.BuildKey(addresses[0].CompanyId, addresses[0].Id),
                RedisCompanyAddressCache.BuildKey(addresses[1].CompanyId, addresses[1].Id),
                RedisCompanyAddressCache.BuildKey(addresses[2].CompanyId, addresses[2].Id)
            ]
        };
        var module = CreateCompanyAddressModule(reader, cache, batchSize: 3, failureThreshold: 2);

        await module.WarmUpAsync(CancellationToken.None);

        Assert.Equal([0], reader.AddressSkips);
        Assert.Equal(2, cache.SetCalls.Count);
    }

    [Fact]
    public async Task CompanyCacheWarmupModule_WhenWriteSucceeds_ResetsConsecutiveFailureCount()
    {
        var companies = new[]
        {
            CreateCompany(Guid.Parse("00000000-0000-0000-0000-000000000001"), "first"),
            CreateCompany(Guid.Parse("00000000-0000-0000-0000-000000000002"), "second"),
            CreateCompany(Guid.Parse("00000000-0000-0000-0000-000000000003"), "third"),
            CreateCompany(Guid.Parse("00000000-0000-0000-0000-000000000004"), "fourth"),
            CreateCompany(Guid.Parse("00000000-0000-0000-0000-000000000005"), "fifth")
        };
        var reader = new FakeCompanyCacheWarmupReader(companies: companies);
        var cache = new FakeCacheService
        {
            FailedKeys =
            [
                RedisCompanyCache.BuildKey(companies[0].Id),
                RedisCompanyCache.BuildKey(companies[2].Id),
                RedisCompanyCache.BuildKey(companies[3].Id),
                RedisCompanyCache.BuildKey(companies[4].Id)
            ]
        };
        var module = CreateCompanyModule(reader, cache, batchSize: 5, failureThreshold: 2);

        await module.WarmUpAsync(CancellationToken.None);

        Assert.Equal(4, cache.SetCalls.Count);
    }

    [Fact]
    public async Task CompanyCacheWarmupModule_WhenFailuresAreNotConsecutive_DoesNotStopEarly()
    {
        var companies = new[]
        {
            CreateCompany(Guid.Parse("00000000-0000-0000-0000-000000000001"), "first"),
            CreateCompany(Guid.Parse("00000000-0000-0000-0000-000000000002"), "second"),
            CreateCompany(Guid.Parse("00000000-0000-0000-0000-000000000003"), "third"),
            CreateCompany(Guid.Parse("00000000-0000-0000-0000-000000000004"), "fourth")
        };
        var reader = new FakeCompanyCacheWarmupReader(companies: companies);
        var cache = new FakeCacheService
        {
            FailedKeys =
            [
                RedisCompanyCache.BuildKey(companies[0].Id),
                RedisCompanyCache.BuildKey(companies[2].Id)
            ]
        };
        var module = CreateCompanyModule(reader, cache, batchSize: 4, failureThreshold: 2);

        await module.WarmUpAsync(CancellationToken.None);

        Assert.Equal(4, cache.SetCalls.Count);
    }

    [Fact]
    public async Task CompanyCacheWarmupModule_WhenCompletedSuccessfully_LogsSuccessfulCompletion()
    {
        var reader = new FakeCompanyCacheWarmupReader(companies: [CreateCompany("first")]);
        var logger = new TestLogger<CompanyCacheWarmupModule>();
        var module = CreateCompanyModule(reader, new FakeCacheService(), batchSize: 2, logger: logger);

        await module.WarmUpAsync(CancellationToken.None);

        Assert.Contains(logger.Messages, message => message.Contains("completed successfully", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Messages, message => message.Contains("stopped after reaching", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CompanyAddressCacheWarmupModule_WhenCancellationIsRequested_PropagatesCancellation()
    {
        var logger = new TestLogger<CompanyAddressCacheWarmupModule>();
        var reader = new FakeCompanyCacheWarmupReader(addresses: [CreateAddress()])
        {
            ThrowWhenCancellationRequested = true
        };
        var module = CreateCompanyAddressModule(reader, new FakeCacheService(), batchSize: 2, logger: logger);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            module.WarmUpAsync(cancellation.Token));

        Assert.Contains(logger.Messages, message => message.Contains("cancelled", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Messages, message => message.Contains("completed successfully", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CompanyCacheWarmupModule_WhenCacheWriteIsCanceled_PropagatesCancellation()
    {
        var reader = new FakeCompanyCacheWarmupReader(companies: [CreateCompany("first")]);
        var module = CreateCompanyModule(reader, new FakeCacheService(), batchSize: 1);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            module.WarmUpAsync(cancellation.Token));
    }

    [Fact]
    public async Task CompanyCacheWarmupModule_WhenReaderFails_LogsFailureAndPropagatesException()
    {
        var logger = new TestLogger<CompanyCacheWarmupModule>();
        var reader = new FakeCompanyCacheWarmupReader
        {
            Exception = new InvalidOperationException("read failed")
        };
        var module = CreateCompanyModule(reader, new FakeCacheService(), batchSize: 2, logger: logger);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            module.WarmUpAsync(CancellationToken.None));

        Assert.Contains(logger.Messages, message => message.Contains("failed", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Messages, message => message.Contains("completed successfully", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("CompanyCacheWarmup:ConsecutiveCacheWriteFailureThreshold")]
    [InlineData("CompanyAddressCacheWarmup:ConsecutiveCacheWriteFailureThreshold")]
    public void AddCacheWorker_WhenConsecutiveCacheWriteFailureThresholdIsInvalid_RejectsOptions(
        string optionName)
    {
        var configuration = CreateCacheWorkerConfiguration(new Dictionary<string, string?>
        {
            [optionName] = "0"
        });
        var services = new ServiceCollection();

        services.AddCacheWorker(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        Assert.Throws<OptionsValidationException>(() => GetInvalidWarmupOptions(serviceProvider, optionName));
    }

    public static IEnumerable<object[]> InvalidOptions()
    {
        yield return [new CacheWorkerOptions { RefreshInterval = TimeSpan.Zero }];
        yield return [new CacheWorkerOptions { GlobalTimeout = TimeSpan.Zero }];
        yield return [new CacheWorkerOptions { MaxDegreeOfParallelism = 0 }];
        yield return [new CacheWorkerOptions { StartupJitterPercentage = -1 }];
        yield return [new CacheWorkerOptions { StartupJitterPercentage = 51 }];
        yield return [new CacheWorkerOptions { RefreshJitterPercentage = double.NaN }];
        yield return [new CacheWorkerOptions { RefreshJitterPercentage = double.PositiveInfinity }];
    }

    private static CacheWorkerBackgroundService CreateService(
        IReadOnlyList<ICacheWarmupModule> modules,
        IHostApplicationLifetime? applicationLifetime = null,
        CacheWorkerOptions? options = null)
    {
        return new CacheWorkerBackgroundService(
            modules,
            Options.Create(options ?? new CacheWorkerOptions()),
            applicationLifetime ?? new FakeHostApplicationLifetime(),
            NullLogger<CacheWorkerBackgroundService>.Instance);
    }

    private static CompanyCacheWarmupModule CreateCompanyModule(
        ICompanyCacheWarmupReader reader,
        ICacheService cache,
        int batchSize,
        int failureThreshold = 10,
        ILogger<CompanyCacheWarmupModule>? logger = null)
    {
        return new CompanyCacheWarmupModule(
            reader,
            cache,
            Options.Create(new CompanyCacheWarmupOptions
            {
                BatchSize = batchSize,
                ConsecutiveCacheWriteFailureThreshold = failureThreshold
            }),
            logger ?? NullLogger<CompanyCacheWarmupModule>.Instance);
    }

    private static CompanyAddressCacheWarmupModule CreateCompanyAddressModule(
        ICompanyCacheWarmupReader reader,
        ICacheService cache,
        int batchSize,
        int failureThreshold = 10,
        ILogger<CompanyAddressCacheWarmupModule>? logger = null)
    {
        return new CompanyAddressCacheWarmupModule(
            reader,
            cache,
            Options.Create(new CompanyAddressCacheWarmupOptions
            {
                BatchSize = batchSize,
                ConsecutiveCacheWriteFailureThreshold = failureThreshold
            }),
            logger ?? NullLogger<CompanyAddressCacheWarmupModule>.Instance);
    }

    private static IConfiguration CreateCacheWorkerConfiguration(
        IDictionary<string, string?> overrides)
    {
        var values = new Dictionary<string, string?>(overrides)
        {
            ["ConnectionStrings:CompanyDb"] = "Server=localhost;Database=CompanyDb;Trusted_Connection=True;",
            ["ConnectionStrings:Redis"] = "localhost:6379"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private static object GetInvalidWarmupOptions(
        IServiceProvider serviceProvider,
        string optionName)
    {
        return optionName.StartsWith("CompanyAddressCacheWarmup:", StringComparison.Ordinal)
            ? serviceProvider.GetRequiredService<IOptions<CompanyAddressCacheWarmupOptions>>().Value
            : serviceProvider.GetRequiredService<IOptions<CompanyCacheWarmupOptions>>().Value;
    }

    private static CompanyResult CreateCompany(string name)
        => CreateCompany(Guid.NewGuid(), name);

    private static CompanyResult CreateCompany(Guid id, string name)
    {
        return new CompanyResult(
            id,
            name,
            name.ToUpperInvariant(),
            CompanyStatus.Active,
            DateTime.UtcNow,
            null);
    }

    private static CompanyAddressResult CreateAddress()
        => CreateAddress(Guid.NewGuid());

    private static CompanyAddressResult CreateAddress(Guid addressId)
    {
        return new CompanyAddressResult(
            addressId,
            Guid.NewGuid(),
            CompanyAddressType.Shipping,
            "US",
            "New York",
            "10001",
            "1 Logistics Way",
            null,
            DateTime.UtcNow,
            null);
    }

    private sealed class FakeWarmupModule : ICacheWarmupModule
    {
        public FakeWarmupModule(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public int CallCount { get; private set; }

        public Exception? Exception { get; init; }

        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource? Release { get; init; }

        public bool ObservedCancellation { get; private set; }

        public async Task WarmUpAsync(CancellationToken cancellationToken)
        {
            CallCount++;
            Started.SetResult();

            if (Exception is not null)
            {
                throw Exception;
            }

            if (Release is null)
            {
                return;
            }

            try
            {
                await Release.Task.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                ObservedCancellation = true;
                throw;
            }
        }
    }

    private sealed class FakeCompanyCacheWarmupReader : ICompanyCacheWarmupReader
    {
        private readonly IReadOnlyList<CompanyResult> _companies;
        private readonly IReadOnlyList<CompanyAddressResult> _addresses;

        public FakeCompanyCacheWarmupReader(
            IReadOnlyList<CompanyResult>? companies = null,
            IReadOnlyList<CompanyAddressResult>? addresses = null)
        {
            _companies = companies ?? [];
            _addresses = addresses ?? [];
        }

        public List<int> CompanySkips { get; } = [];

        public List<int> AddressSkips { get; } = [];

        public bool ThrowWhenCancellationRequested { get; init; }

        public Exception? Exception { get; init; }

        public Task<IReadOnlyList<CompanyResult>> ReadCompaniesAsync(
            int skip,
            int take,
            CancellationToken cancellationToken)
        {
            if (Exception is not null)
            {
                throw Exception;
            }

            if (ThrowWhenCancellationRequested)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            CompanySkips.Add(skip);
            return Task.FromResult<IReadOnlyList<CompanyResult>>(
                _companies.Skip(skip).Take(take).ToArray());
        }

        public Task<IReadOnlyList<CompanyAddressResult>> ReadCompanyAddressesAsync(
            int skip,
            int take,
            CancellationToken cancellationToken)
        {
            if (Exception is not null)
            {
                throw Exception;
            }

            if (ThrowWhenCancellationRequested)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            AddressSkips.Add(skip);
            return Task.FromResult<IReadOnlyList<CompanyAddressResult>>(
                _addresses.Skip(skip).Take(take).ToArray());
        }
    }

    private sealed class FakeCacheService : ICacheService
    {
        public List<SetCall> SetCalls { get; } = [];

        public HashSet<string> FailedKeys { get; init; } = [];

        public Task<T?> GetOrCreateAsync<T>(
            string key,
            Func<CancellationToken, Task<T?>> sourceFactory,
            TimeSpan? ttl = null,
            CancellationToken cancellationToken = default)
            where T : class
        {
            return sourceFactory(cancellationToken);
        }

        public Task<bool> SetAsync<T>(
            string key,
            T? value,
            TimeSpan? ttl = null,
            CancellationToken cancellationToken = default)
            where T : class
        {
            cancellationToken.ThrowIfCancellationRequested();
            SetCalls.Add(new SetCall(key, typeof(T)));

            return Task.FromResult(!FailedKeys.Contains(key));
        }

        public Task RemoveAsync(
            string key,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed record SetCall(string Key, Type ValueType);

    private sealed class TestLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }

    private sealed class TrackingWarmupModule : ICacheWarmupModule
    {
        private readonly ConcurrencyTracker _tracker;
        private readonly TaskCompletionSource _release;

        public TrackingWarmupModule(
            string name,
            ConcurrencyTracker tracker,
            TaskCompletionSource release)
        {
            Name = name;
            _tracker = tracker;
            _release = release;
        }

        public string Name { get; }

        public async Task WarmUpAsync(CancellationToken cancellationToken)
        {
            _tracker.Start();
            try
            {
                await _release.Task.WaitAsync(cancellationToken);
            }
            finally
            {
                _tracker.Stop();
            }
        }
    }

    private sealed class ConcurrencyTracker
    {
        private readonly object _gate = new();
        private readonly TaskCompletionSource _targetActiveCountReached = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _activeCount;

        public int MaxActiveCount { get; private set; }

        public int TotalStarted { get; private set; }

        public void Start()
        {
            var activeCount = Interlocked.Increment(ref _activeCount);
            lock (_gate)
            {
                TotalStarted++;
                MaxActiveCount = Math.Max(MaxActiveCount, activeCount);
            }

            if (activeCount == 2)
            {
                _targetActiveCountReached.TrySetResult();
            }
        }

        public void Stop()
        {
            Interlocked.Decrement(ref _activeCount);
        }

        public Task WaitForActiveCountAsync(int activeCount)
        {
            return activeCount == 2
                ? _targetActiveCountReached.Task
                : throw new InvalidOperationException("Only the configured test target is supported.");
        }
    }

    private sealed class FakeHostApplicationLifetime : IHostApplicationLifetime
    {
        private readonly CancellationTokenSource _applicationStarted = new();
        private readonly CancellationTokenSource _applicationStopping = new();
        private readonly CancellationTokenSource _applicationStopped = new();

        public CancellationToken ApplicationStarted => _applicationStarted.Token;

        public CancellationToken ApplicationStopping => _applicationStopping.Token;

        public CancellationToken ApplicationStopped => _applicationStopped.Token;

        public int StopApplicationCallCount { get; private set; }

        public TaskCompletionSource StopRequested { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void StopApplication()
        {
            StopApplicationCallCount++;
            StopRequested.SetResult();
            _applicationStopping.Cancel();
        }
    }
}
