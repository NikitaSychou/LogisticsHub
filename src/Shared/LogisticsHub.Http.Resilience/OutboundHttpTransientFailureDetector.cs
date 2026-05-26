using System.Net;

namespace LogisticsHub.Http.Resilience;

public static class OutboundHttpTransientFailureDetector
{
    public static bool IsTransient(HttpResponseMessage response)
    {
        return response.StatusCode == HttpStatusCode.RequestTimeout ||
            (int)response.StatusCode >= (int)HttpStatusCode.InternalServerError;
    }

    public static bool IsTransient(Exception exception, CancellationToken cancellationToken)
    {
        return exception is HttpRequestException ||
            exception is TaskCanceledException && !cancellationToken.IsCancellationRequested;
    }
}
