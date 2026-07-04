using Microsoft.Extensions.Options;

namespace LogisticsHub.Workers.CacheWorker;

public sealed class CacheWorkerBackgroundService : BackgroundService
{
    private readonly IReadOnlyList<ICacheWarmupModule> _modules;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly ILogger<CacheWorkerBackgroundService> _logger;
    private readonly CacheWorkerOptions _options;

    public CacheWorkerBackgroundService(
        IEnumerable<ICacheWarmupModule> modules,
        IOptions<CacheWorkerOptions> options,
        IHostApplicationLifetime applicationLifetime,
        ILogger<CacheWorkerBackgroundService> logger)
    {
        _modules = modules.ToArray();
        _options = options.Value;
        _applicationLifetime = applicationLifetime;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            if (_options.RunOnStartup)
            {
                await DelayWithJitterAsync(
                    TimeSpan.Zero,
                    _options.StartupJitterPercentage,
                    stoppingToken);
                await RunWarmupAsync(stoppingToken);
            }

            if (_options.RunOnce)
            {
                _applicationLifetime.StopApplication();
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                await DelayWithJitterAsync(
                    _options.RefreshInterval,
                    _options.RefreshJitterPercentage,
                    stoppingToken);
                await RunWarmupAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Cache worker stopped.");
        }
    }

    internal async Task RunWarmupAsync(CancellationToken cancellationToken)
    {
        var modules = GetEnabledModules();
        if (modules.Count == 0)
        {
            _logger.LogInformation("Cache worker run skipped because no warm-up modules are registered or enabled.");
            return;
        }

        _logger.LogInformation("Cache worker run started with {ModuleCount} module(s).", modules.Count);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_options.GlobalTimeout);

        try
        {
            await RunModulesAsync(modules, timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && timeout.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Cache worker run timed out after {GlobalTimeout}.",
                _options.GlobalTimeout);
            return;
        }

        _logger.LogInformation("Cache worker run completed.");
    }

    private IReadOnlyList<ICacheWarmupModule> GetEnabledModules()
    {
        if (_options.EnabledModules.Length == 0)
        {
            return _modules;
        }

        var enabledModules = _options.EnabledModules.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var module in _modules.Where(module => !enabledModules.Contains(module.Name)))
        {
            _logger.LogInformation("Cache warm-up module {ModuleName} skipped by configuration.", module.Name);
        }

        return _modules
            .Where(module => enabledModules.Contains(module.Name))
            .ToArray();
    }

    private async Task RunModulesAsync(
        IReadOnlyList<ICacheWarmupModule> modules,
        CancellationToken cancellationToken)
    {
        using var semaphore = new SemaphoreSlim(_options.MaxDegreeOfParallelism);
        var tasks = modules
            .Select(module => RunModuleWithConcurrencyLimitAsync(module, semaphore, cancellationToken))
            .ToArray();

        await Task.WhenAll(tasks);
    }

    private async Task RunModuleWithConcurrencyLimitAsync(
        ICacheWarmupModule module,
        SemaphoreSlim semaphore,
        CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            await RunModuleAsync(module, cancellationToken);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task RunModuleAsync(
        ICacheWarmupModule module,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Cache warm-up module {ModuleName} started.", module.Name);
            await module.WarmUpAsync(cancellationToken);
            _logger.LogInformation("Cache warm-up module {ModuleName} completed.", module.Name);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Cache warm-up module {ModuleName} cancelled.", module.Name);
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Cache warm-up module {ModuleName} failed.", module.Name);
        }
    }

    private static async Task DelayWithJitterAsync(
        TimeSpan baseDelay,
        double jitterPercentage,
        CancellationToken cancellationToken)
    {
        var delay = GetDelayWithJitter(baseDelay, jitterPercentage);
        if (delay > TimeSpan.Zero)
        {
            await Task.Delay(delay, cancellationToken);
        }
    }

    private static TimeSpan GetDelayWithJitter(
        TimeSpan baseDelay,
        double jitterPercentage)
    {
        if (baseDelay <= TimeSpan.Zero || jitterPercentage == 0)
        {
            return baseDelay;
        }

        var maximumOffsetTicks = baseDelay.Ticks * (jitterPercentage / 100);
        var offsetTicks = (long)Math.Round((Random.Shared.NextDouble() * 2 - 1) * maximumOffsetTicks);
        var delayTicks = Math.Max(1, baseDelay.Ticks + offsetTicks);

        return TimeSpan.FromTicks(delayTicks);
    }
}
