using LogisticsHub.InventoryService.Application.Persistence;
using LogisticsHub.InventoryService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogisticsHub.InventoryService.Infrastructure.Persistence;

public class InventoryDbContext : DbContext, IInventoryDbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options)
    {
    }

    public DbSet<Item> Items { get; set; }
    public DbSet<StockBalance> StockBalances { get; set; }
    public DbSet<StockReservation> StockReservations { get; set; }
    public DbSet<StockReservationItem> StockReservationItems { get; set; }

    public async Task<Item?> GetItemBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        return await Items
            .AsNoTracking()
            .Include(item => item.StockBalance)
            .SingleOrDefaultAsync(item => item.Sku == sku, cancellationToken);
    }

    public async Task<IReadOnlyList<Item>> GetItemsBySkusAsync(
        IReadOnlyCollection<string> skus,
        CancellationToken cancellationToken = default)
    {
        return await Items
            .Include(item => item.StockBalance)
            .Where(item => skus.Contains(item.Sku))
            .ToListAsync(cancellationToken);
    }

    public async Task<StockReservation?> GetStockReservationByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await StockReservations
            .AsNoTracking()
            .Include(stockReservation => stockReservation.Items)
            .ThenInclude(stockReservationItem => stockReservationItem.Item)
            .SingleOrDefaultAsync(stockReservation => stockReservation.Id == id, cancellationToken);
    }

    public async Task AddItemAsync(Item item, CancellationToken cancellationToken = default)
    {
        await Items.AddAsync(item, cancellationToken);
    }

    public async Task AddStockBalanceAsync(StockBalance stockBalance, CancellationToken cancellationToken = default)
    {
        await StockBalances.AddAsync(stockBalance, cancellationToken);
    }

    public async Task AddStockReservationAsync(
        StockReservation stockReservation,
        CancellationToken cancellationToken = default)
    {
        await StockReservations.AddAsync(stockReservation, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Item>(builder =>
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
        });

        modelBuilder.Entity<StockBalance>(builder =>
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
        });

        modelBuilder.Entity<StockReservation>(builder =>
        {
            builder.ToTable("stock_reservations", "dbo");

            builder.HasKey(stockReservation => stockReservation.Id);

            builder.Property(stockReservation => stockReservation.Id)
                .HasColumnName("id");

            builder.Property(stockReservation => stockReservation.ShipmentId)
                .HasColumnName("shipment_id");

            builder.Property(stockReservation => stockReservation.Status)
                .HasColumnName("status")
                .HasMaxLength(32)
                .HasConversion<string>();

            builder.Property(stockReservation => stockReservation.CreatedAt)
                .HasColumnName("created_at");

            builder.Property(stockReservation => stockReservation.UpdatedAt)
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<StockReservationItem>(builder =>
        {
            builder.ToTable("stock_reservation_items", "dbo");

            builder.HasKey(stockReservationItem => new
            {
                stockReservationItem.ReservationId,
                stockReservationItem.ItemId
            });

            builder.Property(stockReservationItem => stockReservationItem.ReservationId)
                .HasColumnName("reservation_id");

            builder.Property(stockReservationItem => stockReservationItem.ItemId)
                .HasColumnName("item_id");

            builder.Property(stockReservationItem => stockReservationItem.Quantity)
                .HasColumnName("quantity");

            builder.HasOne(stockReservationItem => stockReservationItem.Reservation)
                .WithMany(stockReservation => stockReservation.Items)
                .HasForeignKey(stockReservationItem => stockReservationItem.ReservationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(stockReservationItem => stockReservationItem.Item)
                .WithMany()
                .HasForeignKey(stockReservationItem => stockReservationItem.ItemId);
        });
    }
}
