using LogisticsHub.CompanyService.Domain.Enums;
using LogisticsHub.Results;
using MediatR;

namespace LogisticsHub.CompanyService.Application.Companies;

public sealed record UpdateCompanyCommand(
    Guid Id,
    string Name,
    string? ExternalCode,
    CompanyStatus Status) : IRequest<Result<CompanyResult>>;
