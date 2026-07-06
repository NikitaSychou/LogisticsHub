using LogisticsHub.CompanyService.Domain.Enums;

namespace LogisticsHub.CompanyService.Application.Companies.Results;

public sealed record CompanyResult(
    Guid Id,
    string Name,
    string? ExternalCode,
    CompanyStatus Status,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
