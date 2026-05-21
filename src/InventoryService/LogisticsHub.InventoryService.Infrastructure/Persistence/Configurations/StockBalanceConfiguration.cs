using LogisticsHub.InventoryService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsHub.InventoryService.Infrastructure.Persistence.Configurations;

public sealed class StockBalanceConfiguration : IEntityTypeConfiguration<StockBalance>
{
    public void Configure(EntityTypeBuilder<StockBalance> builder)
    {
        builder.ToTable("stock_balances", "dbo");

        builder.HasKey(stockBalance => stockBalance.ItemId);

        builder.Property(stockBalance => stockBalance.ItemId)
            .HasColumnName("item_id");

        builder.Property(stockBalance => stockBalance.OnHand)
            .HasColumnName("on_hand");

        builder.Property(stockBalance => stockBalance.Reserved)
            .HasColumnName("reserved");

        builder.Property(stockBalance => stockBalance.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(stockBalance => stockBalance.RowVersion)
            .HasColumnName("row_version")
            .IsRowVersion();
    }
}
