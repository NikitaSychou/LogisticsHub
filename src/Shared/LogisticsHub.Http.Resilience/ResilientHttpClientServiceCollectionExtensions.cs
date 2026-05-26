using Microsoft.Extensions.DependencyInjection;

namespace LogisticsHub.Http.Resilience;

public static class ResilientHttpClientServiceCollectionExtensions
{
    public static IHttpClientBuilder AddOutboundHttpResilience(
        this IHttpClientBuilder builder,
        OutboundHttpClientResilienceOptions options)
    {
        var circuitBreakerState = new OutboundHttpCircuitBreakerState();

        return builder
            .AddHttpMessageHandler(() => new OutboundHttpCircuitBreakerHandler(options, circuitBreakerState))
            .AddHttpMessageHandler(() => new OutboundHttpRetryHandler(options));
    }
}
