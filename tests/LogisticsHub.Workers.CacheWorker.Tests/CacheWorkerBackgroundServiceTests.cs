using LogisticsHub.Workers.CacheWorker;
using Microsoft.Extensions.Hosting;
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
