namespace LogisticsHub.Workers.CacheWorker;

public sealed class CacheWorkerOptions
{
    public const double MaximumJitterPercentage = 50;

    public bool RunOnStartup { get; set; } = true;

    public bool RunOnce { get; set; }

    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromHours(24);

    public double StartupJitterPercentage { get; set; } = 5;

    public double RefreshJitterPercentage { get; set; } = 5;

    public string[] EnabledModules { get; set; } = [];

    public TimeSpan GlobalTimeout { get; set; } = TimeSpan.FromMinutes(30);

    public int MaxDegreeOfParallelism { get; set; } = 2;
}
