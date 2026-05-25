namespace LogisticsHub.CompanyService.Contracts;

public sealed record CreateCompanyRequest(
    string? Name,
    string? ExternalCode,
    string? Status);
