using LogisticsHub.CompanyService.Domain.Enums;

namespace LogisticsHub.CompanyService.Domain.Entities;

public sealed class Company
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? ExternalCode { get; set; }

    public CompanyStatus Status { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public List<CompanyAddress> Addresses { get; set; } = [];
}
