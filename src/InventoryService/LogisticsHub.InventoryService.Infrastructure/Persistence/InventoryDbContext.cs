using LogisticsHub.InventoryService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogisticsHub.InventoryService.Infrastructure.Persistence;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options)
    {
    }

    public DbSet<StockReservation> StockReservations { get; set; }
    public DbSet<StockReservationItem> StockReservationItems { get; set; }
}
