namespace LogisticsHub.Http.Resilience;

public sealed class OutboundHttpCircuitBreakerState
{
    private readonly object _gate = new();
    private int _consecutiveFailures;
    private DateTimeOffset? _openUntilUtc;

    public bool IsOpen()
    {
        lock (_gate)
        {
            if (_openUntilUtc is null)
            {
                return false;
            }

            if (DateTimeOffset.UtcNow < _openUntilUtc)
            {
                return true;
            }

            _openUntilUtc = null;
            _consecutiveFailures = 0;
            return false;
        }
    }

    public void RecordSuccess()
    {
        lock (_gate)
        {
            _consecutiveFailures = 0;
            _openUntilUtc = null;
        }
    }

    public void RecordFailure(int failureThreshold, TimeSpan breakDuration)
    {
        lock (_gate)
        {
            _consecutiveFailures++;

            if (_consecutiveFailures >= failureThreshold)
            {
                _openUntilUtc = DateTimeOffset.UtcNow.Add(breakDuration);
            }
        }
    }
}
