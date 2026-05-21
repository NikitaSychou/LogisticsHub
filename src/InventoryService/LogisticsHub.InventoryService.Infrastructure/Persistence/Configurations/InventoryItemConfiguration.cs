using LogisticsHub.InventoryService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsHub.InventoryService.Infrastructure.Persistence.Configurations;

public sealed class InventoryItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.ToTable("items", "dbo");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id)
            .HasColumnName("id");

        builder.Property(item => item.Sku)
            .HasColumnName("sku")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(item => item.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(item => item.IsActive)
            .HasColumnName("is_active");

        builder.Property(item => item.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(item => item.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasOne(item => item.StockBalance)
            .WithOne(stockBalance => stockBalance.Item)
            .HasForeignKey<StockBalance>(stockBalance => stockBalance.ItemId);
    }
}
