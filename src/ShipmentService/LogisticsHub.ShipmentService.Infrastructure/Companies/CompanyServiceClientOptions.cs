using LogisticsHub.Http.Resilience;

namespace LogisticsHub.ShipmentService.Infrastructure.Companies;

public sealed class CompanyServiceClientOptions
{
    public const string SectionName = "CompanyService";

    public string? BaseUrl { get; init; }

    public OutboundHttpClientResilienceOptions Resilience { get; init; } = new();
}
