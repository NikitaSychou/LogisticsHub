using LogisticsHub.CompanyService.Domain.Enums;

namespace LogisticsHub.CompanyService.Domain.Entities;

public sealed class CompanyAddress
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }

    public CompanyAddressType AddressType { get; set; }

    public string CountryCode { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string? PostalCode { get; set; }

    public string Line1 { get; set; } = string.Empty;

    public string? Line2 { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public Company Company { get; set; } = null!;
}
