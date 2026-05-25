namespace LogisticsHub.CompanyService.Contracts;

public sealed record UpdateCompanyRequest(
    string? Name,
    string? ExternalCode,
    string? Status);
