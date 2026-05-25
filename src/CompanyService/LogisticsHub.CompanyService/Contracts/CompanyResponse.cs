using LogisticsHub.CompanyService.Domain.Enums;

namespace LogisticsHub.CompanyService.Contracts;

public sealed record CompanyResponse(
    Guid Id,
    string Name,
    string? ExternalCode,
    CompanyStatus Status,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
