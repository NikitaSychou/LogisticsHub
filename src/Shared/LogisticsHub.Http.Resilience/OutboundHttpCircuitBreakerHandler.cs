namespace LogisticsHub.Http.Resilience;

public sealed class OutboundHttpCircuitBreakerHandler : DelegatingHandler
{
    private readonly OutboundHttpClientResilienceOptions _options;
    private readonly OutboundHttpCircuitBreakerState _state;

    public OutboundHttpCircuitBreakerHandler(
        OutboundHttpClientResilienceOptions options,
        OutboundHttpCircuitBreakerState state)
    {
        _options = options;
        _state = state;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (_state.IsOpen())
        {
            throw new HttpRequestException("Outbound HTTP circuit breaker is open.");
        }

        try
        {
            var response = await base.SendAsync(request, cancellationToken);

            if (OutboundHttpTransientFailureDetector.IsTransient(response))
            {
                _state.RecordFailure(
                    _options.CircuitBreakerFailureThreshold,
                    _options.CircuitBreakerDuration);
            }
            else
            {
                _state.RecordSuccess();
            }

            return response;
        }
        catch (Exception exception)
            when (OutboundHttpTransientFailureDetector.IsTransient(exception, cancellationToken))
        {
            _state.RecordFailure(
                _options.CircuitBreakerFailureThreshold,
                _options.CircuitBreakerDuration);
            throw;
        }
    }
}
