namespace LogisticsHub.Http.Resilience;

public sealed class OutboundHttpRetryHandler : DelegatingHandler
{
    private readonly OutboundHttpClientResilienceOptions _options;

    public OutboundHttpRetryHandler(OutboundHttpClientResilienceOptions options)
    {
        _options = options;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                var response = await base.SendAsync(request, cancellationToken);

                if (!OutboundHttpTransientFailureDetector.IsTransient(response) ||
                    attempt >= _options.RetryCount)
                {
                    return response;
                }

                response.Dispose();
            }
            catch (Exception exception)
                when (OutboundHttpTransientFailureDetector.IsTransient(exception, cancellationToken) &&
                    attempt < _options.RetryCount)
            {
            }

            await Task.Delay(_options.RetryDelay, cancellationToken);
        }
    }
}
