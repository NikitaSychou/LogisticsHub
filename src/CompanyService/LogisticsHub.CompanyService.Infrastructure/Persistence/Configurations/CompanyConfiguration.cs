using LogisticsHub.CompanyService.Domain.Entities;
using LogisticsHub.CompanyService.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsHub.CompanyService.Infrastructure.Persistence.Configurations;

public sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("companies", "dbo", table =>
        {
            table.HasCheckConstraint("CK_companies_status", "[status]=N'Inactive' OR [status]=N'Active'");
            table.HasCheckConstraint("CK_companies_name_not_empty", "len(ltrim(rtrim([name])))>(0)");
        });

        builder.HasKey(company => company.Id);

        builder.Property(company => company.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("newsequentialid()");

        builder.Property(company => company.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(company => company.ExternalCode)
            .HasColumnName("external_code")
            .HasMaxLength(64);

        builder.Property(company => company.Status)
            .HasColumnName("status")
            .HasMaxLength(32)
            .HasConversion<string>()
            .HasDefaultValue(CompanyStatus.Active)
            .IsRequired();

        builder.Property(company => company.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("datetime2(7)")
            .HasDefaultValueSql("sysutcdatetime()");

        builder.Property(company => company.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasColumnType("datetime2(7)");

        builder.HasIndex(company => company.Name)
            .HasDatabaseName("IX_Companies_Name");

        builder.HasIndex(company => company.ExternalCode)
            .HasDatabaseName("UX_Companies_ExternalCode")
            .IsUnique()
            .HasFilter("[external_code] IS NOT NULL");

        builder.HasMany(company => company.Addresses)
            .WithOne(address => address.Company)
            .HasForeignKey(address => address.CompanyId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
