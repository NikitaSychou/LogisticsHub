using LogisticsHub.CompanyService.Application.Persistence;
using LogisticsHub.CompanyService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogisticsHub.CompanyService.Infrastructure.Persistence;

public sealed class CompanyDbContext : DbContext, ICompanyDbContext
{
    public CompanyDbContext(DbContextOptions<CompanyDbContext> options) : base(options)
    {
    }

    public DbSet<Company> Companies { get; set; }

    public DbSet<CompanyAddress> CompanyAddresses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CompanyDbContext).Assembly);
    }
}
