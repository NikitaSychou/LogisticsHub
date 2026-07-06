using LogisticsHub.CompanyService.Application.Companies.Results;
using LogisticsHub.CompanyService.Domain.Enums;
using LogisticsHub.Results;
using MediatR;

namespace LogisticsHub.CompanyService.Application.Companies.Company.CreateCompany;

public sealed record CreateCompanyCommand(
    string Name,
    string? ExternalCode,
    CompanyStatus Status) : IRequest<Result<CompanyResult>>;
