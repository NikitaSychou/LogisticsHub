using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace LogisticsHub.ShipmentService.Infrastructure.Companies;

public sealed class ForwardUserBearerTokenHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ForwardUserBearerTokenHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var authorization = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();

        if (!AuthenticationHeaderValue.TryParse(authorization, out var header) ||
            !string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(header.Parameter))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                RequestMessage = request
            });
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", header.Parameter);

        return base.SendAsync(request, cancellationToken);
    }
}
