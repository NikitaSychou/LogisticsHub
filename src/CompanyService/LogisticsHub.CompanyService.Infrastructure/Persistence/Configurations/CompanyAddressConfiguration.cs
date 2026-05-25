using LogisticsHub.CompanyService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsHub.CompanyService.Infrastructure.Persistence.Configurations;

public sealed class CompanyAddressConfiguration : IEntityTypeConfiguration<CompanyAddress>
{
    public void Configure(EntityTypeBuilder<CompanyAddress> builder)
    {
        builder.ToTable("company_addresses", "dbo", table =>
        {
            table.HasCheckConstraint(
                "CK_company_addresses_address_type",
                "[address_type]=N'Warehouse' OR [address_type]=N'Shipping' OR [address_type]=N'Billing' OR [address_type]=N'Legal'");
            table.HasCheckConstraint("CK_company_addresses_country_code_length", "len([country_code])=(2)");
            table.HasCheckConstraint("CK_company_addresses_city_not_empty", "len(ltrim(rtrim([city])))>(0)");
            table.HasCheckConstraint("CK_company_addresses_line1_not_empty", "len(ltrim(rtrim([line1])))>(0)");
        });

        builder.HasKey(address => address.Id);

        builder.Property(address => address.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("newsequentialid()");

        builder.Property(address => address.CompanyId)
            .HasColumnName("company_id");

        builder.Property(address => address.AddressType)
            .HasColumnName("address_type")
            .HasMaxLength(32)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(address => address.CountryCode)
            .HasColumnName("country_code")
            .HasMaxLength(2)
            .IsRequired();

        builder.Property(address => address.City)
            .HasColumnName("city")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(address => address.PostalCode)
            .HasColumnName("postal_code")
            .HasMaxLength(32);

        builder.Property(address => address.Line1)
            .HasColumnName("line1")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(address => address.Line2)
            .HasColumnName("line2")
            .HasMaxLength(200);

        builder.Property(address => address.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("datetime2(7)")
            .HasDefaultValueSql("sysutcdatetime()");

        builder.Property(address => address.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasColumnType("datetime2(7)");

        builder.HasIndex(address => address.CompanyId)
            .HasDatabaseName("IX_CompanyAddresses_CompanyId");

        builder.HasIndex(address => address.AddressType)
            .HasDatabaseName("IX_CompanyAddresses_AddressType");
    }
}
