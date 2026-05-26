namespace LogisticsHub.InventoryService.Outbox;

public sealed class InventoryOutboxPublisherBackgroundService : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan LockTimeout = TimeSpan.FromMinutes(5);
    private const int BatchSize = 20;

    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<InventoryOutboxPublisherBackgroundService> _logger;
    private readonly string _instanceId = $"{Environment.MachineName}-{Guid.NewGuid():N}";

    public InventoryOutboxPublisherBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<InventoryOutboxPublisherBackgroundService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processedAny = await ProcessBatchAsync(stoppingToken);

                if (!processedAny)
                {
                    await Task.Delay(PollingInterval, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to process inventory outbox messages.");
                await Task.Delay(PollingInterval, stoppingToken);
            }
        }
    }

    private async Task<bool> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();

        var processor = scope.ServiceProvider.GetRequiredService<InventoryOutboxProcessor>();
        var result = await processor.ProcessBatchAsync(
            BatchSize,
            _instanceId,
            LockTimeout,
            cancellationToken);

        if (result.HadFailure)
        {
            await Task.Delay(PollingInterval, cancellationToken);
        }

        return result.ProcessedAny;
    }
}
