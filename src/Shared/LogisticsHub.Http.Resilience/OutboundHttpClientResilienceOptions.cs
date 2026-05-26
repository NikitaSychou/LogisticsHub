namespace LogisticsHub.Http.Resilience;

public sealed class OutboundHttpClientResilienceOptions
{
    public int TimeoutSeconds { get; init; } = 3;

    public int RetryCount { get; init; } = 1;

    public int RetryDelayMilliseconds { get; init; } = 150;

    public int CircuitBreakerFailureThreshold { get; init; } = 3;

    public int CircuitBreakerDurationSeconds { get; init; } = 5;

    public TimeSpan Timeout => TimeSpan.FromSeconds(TimeoutSeconds);

    public TimeSpan RetryDelay => TimeSpan.FromMilliseconds(RetryDelayMilliseconds);

    public TimeSpan CircuitBreakerDuration => TimeSpan.FromSeconds(CircuitBreakerDurationSeconds);

    public void Validate(string sectionName)
    {
        if (TimeoutSeconds <= 0)
        {
            throw new InvalidOperationException($"{sectionName}:TimeoutSeconds must be greater than zero.");
        }

        if (RetryCount < 0)
        {
            throw new InvalidOperationException($"{sectionName}:RetryCount must be zero or greater.");
        }

        if (RetryDelayMilliseconds < 0)
        {
            throw new InvalidOperationException($"{sectionName}:RetryDelayMilliseconds must be zero or greater.");
        }

        if (CircuitBreakerFailureThreshold <= 0)
        {
            throw new InvalidOperationException($"{sectionName}:CircuitBreakerFailureThreshold must be greater than zero.");
        }

        if (CircuitBreakerDurationSeconds <= 0)
        {
            throw new InvalidOperationException($"{sectionName}:CircuitBreakerDurationSeconds must be greater than zero.");
        }
    }
}
