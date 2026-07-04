using System.Net;
using LogisticsHub.Http.Resilience;
using LogisticsHub.ShipmentService.Application.Companies;
using LogisticsHub.ShipmentService.Infrastructure.Companies;
using Xunit;

namespace LogisticsHub.ShipmentService.Application.Tests.Companies;

public sealed class CompanyServiceClientTests
{
    [Fact]
    public async Task ValidateAddressAsync_WhenHttpClientTimesOut_ReturnsDependencyUnavailable()
    {
        var handler = new TimeoutHandler();
        using var httpClient = CreateHttpClient(handler);
        var client = new CompanyServiceClient(httpClient);

        var result = await client.ValidateAddressAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(CompanyAddressReferenceValidationStatus.DependencyUnavailable, result.Status);
    }

    [Fact]
    public async Task OutboundHttpRetryHandler_WhenResponseIsNotFound_DoesNotRetry()
    {
        var sequenceHandler = new SequenceHandler(HttpStatusCode.NotFound);
        using var httpClient = CreateHttpClient(CreateRetryHandler(sequenceHandler));
        var client = new CompanyServiceClient(httpClient);

        var result = await client.ValidateAddressAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(CompanyAddressReferenceValidationStatus.NotFound, result.Status);
        Assert.Equal(1, sequenceHandler.RequestCount);
    }

    [Fact]
    public async Task OutboundHttpRetryHandler_WhenResponseIsTransientFailure_RetriesAndCanSucceed()
    {
        var sequenceHandler = new SequenceHandler(HttpStatusCode.InternalServerError, HttpStatusCode.OK);
        using var httpClient = CreateHttpClient(CreateRetryHandler(sequenceHandler));
        var client = new CompanyServiceClient(httpClient);

        var result = await client.ValidateAddressAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(CompanyAddressReferenceValidationStatus.Found, result.Status);
        Assert.Equal(2, sequenceHandler.RequestCount);
    }

    [Fact]
    public async Task OutboundHttpRetryHandler_WhenRequestIsCanceled_DoesNotRetry()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();
        var innerHandler = new CanceledHandler();
        using var httpClient = CreateHttpClient(CreateRetryHandler(innerHandler));

        await Assert.ThrowsAsync<TaskCanceledException>(
            () => httpClient.GetAsync("companies/test/addresses/test", cancellationTokenSource.Token));

        Assert.Equal(1, innerHandler.RequestCount);
    }

    [Fact]
    public async Task OutboundHttpCircuitBreakerHandler_ObservesFinalResultAfterRetry()
    {
        var innerHandler = new SequenceHandler(
            HttpStatusCode.InternalServerError,
            HttpStatusCode.OK,
            HttpStatusCode.OK);
        var retryHandler = CreateRetryHandler(innerHandler);
        var circuitBreakerHandler = new OutboundHttpCircuitBreakerHandler(
            new OutboundHttpClientResilienceOptions
            {
                CircuitBreakerFailureThreshold = 1,
                CircuitBreakerDurationSeconds = 30
            },
            new OutboundHttpCircuitBreakerState())
        {
            InnerHandler = retryHandler
        };
        using var httpClient = CreateHttpClient(circuitBreakerHandler);

        using var firstResponse = await httpClient.GetAsync("companies/test/addresses/test");
        using var secondResponse = await httpClient.GetAsync("companies/test/addresses/test");

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        Assert.Equal(3, innerHandler.RequestCount);
    }

    [Fact]
    public async Task OutboundHttpCircuitBreakerHandler_AfterTransientFinalFailures_OpensAndSkipsInnerHandler()
    {
        var innerHandler = new SequenceHandler(
            HttpStatusCode.InternalServerError,
            HttpStatusCode.InternalServerError,
            HttpStatusCode.OK);
        var handler = new OutboundHttpCircuitBreakerHandler(
            new OutboundHttpClientResilienceOptions
            {
                CircuitBreakerFailureThreshold = 2,
                CircuitBreakerDurationSeconds = 30
            },
            new OutboundHttpCircuitBreakerState())
        {
            InnerHandler = innerHandler
        };
        using var httpClient = CreateHttpClient(handler);
        var client = new CompanyServiceClient(httpClient);

        var firstResult = await client.ValidateAddressAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);
        var secondResult = await client.ValidateAddressAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);
        var openCircuitResult = await client.ValidateAddressAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(CompanyAddressReferenceValidationStatus.DependencyUnavailable, firstResult.Status);
        Assert.Equal(CompanyAddressReferenceValidationStatus.DependencyUnavailable, secondResult.Status);
        Assert.Equal(CompanyAddressReferenceValidationStatus.DependencyUnavailable, openCircuitResult.Status);
        Assert.Equal(2, innerHandler.RequestCount);
    }

    private static OutboundHttpRetryHandler CreateRetryHandler(HttpMessageHandler innerHandler)
    {
        return new OutboundHttpRetryHandler(
            new OutboundHttpClientResilienceOptions
            {
                RetryCount = 1,
                RetryDelayMilliseconds = 0
            })
        {
            InnerHandler = innerHandler
        };
    }

    private static HttpClient CreateHttpClient(HttpMessageHandler handler)
    {
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("http://companyservice.test/")
        };
    }

    private abstract class CountingHandler : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected sealed override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            return SendCoreAsync(request, cancellationToken);
        }

        protected abstract Task<HttpResponseMessage> SendCoreAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken);
    }

    private sealed class SequenceHandler : CountingHandler
    {
        private readonly Queue<HttpStatusCode> _statusCodes;

        public SequenceHandler(params HttpStatusCode[] statusCodes)
        {
            _statusCodes = new Queue<HttpStatusCode>(statusCodes);
        }

        protected override Task<HttpResponseMessage> SendCoreAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var statusCode = _statusCodes.Count > 0
                ? _statusCodes.Dequeue()
                : HttpStatusCode.OK;

            return Task.FromResult(new HttpResponseMessage(statusCode));
        }
    }

    private sealed class TimeoutHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromException<HttpResponseMessage>(
                new TaskCanceledException("The request timed out."));
        }
    }

    private sealed class CanceledHandler : CountingHandler
    {
        protected override Task<HttpResponseMessage> SendCoreAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromCanceled<HttpResponseMessage>(cancellationToken);
        }
    }
}
