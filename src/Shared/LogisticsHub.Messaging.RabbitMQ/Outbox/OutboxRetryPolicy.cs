namespace LogisticsHub.Messaging.RabbitMQ.Outbox;

public static class OutboxRetryPolicy
{
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromMinutes(15);
    private const int MaxRetryCount = 5;

    public static OutboxRetryDecision RecordFailure(
        int currentRetryCount,
        DateTime failedAtUtc,
        string error)
    {
        var retryCount = currentRetryCount + 1;

        if (retryCount >= MaxRetryCount)
        {
            return new OutboxRetryDecision(
                retryCount,
                error,
                failedAtUtc,
                NextAttemptAtUtc: null,
                ReachedMaxRetryCount: true);
        }

        var retryDelay = GetRetryDelay(retryCount);
        return new OutboxRetryDecision(
            retryCount,
            error,
            FailedAtUtc: null,
            NextAttemptAtUtc: failedAtUtc.Add(retryDelay),
            ReachedMaxRetryCount: false);
    }

    private static TimeSpan GetRetryDelay(int retryCount)
    {
        var seconds = retryCount switch
        {
            1 => 30,
            2 => 60,
            3 => 120,
            4 => 300,
            _ => (int)MaxRetryDelay.TotalSeconds
        };

        return TimeSpan.FromSeconds(seconds);
    }
}

public sealed record OutboxRetryDecision(
    int RetryCount,
    string Error,
    DateTime? FailedAtUtc,
    DateTime? NextAttemptAtUtc,
    bool ReachedMaxRetryCount);
