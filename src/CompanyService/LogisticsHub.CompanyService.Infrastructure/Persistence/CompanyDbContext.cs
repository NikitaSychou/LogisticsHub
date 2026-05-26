using LogisticsHub.CompanyService.Application.Persistence;
using LogisticsHub.CompanyService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogisticsHub.CompanyService.Infrastructure.Persistence;

public sealed class CompanyDbContext : DbContext, ICompanyDbContext
{
    private const string ExternalCodeIndexName = "UX_Companies_ExternalCode";

    public CompanyDbContext(DbContextOptions<CompanyDbContext> options) : base(options)
    {
    }

    public DbSet<Company> Companies { get; set; }

    public DbSet<CompanyAddress> CompanyAddresses { get; set; }

    public async Task<Company?> GetCompanyByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Companies
            .AsNoTracking()
            .SingleOrDefaultAsync(company => company.Id == id, cancellationToken);
    }

    public async Task<Company?> GetCompanyForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Companies
            .SingleOrDefaultAsync(company => company.Id == id, cancellationToken);
    }

    public async Task<Company?> GetCompanyByExternalCodeAsync(
        string externalCode,
        CancellationToken cancellationToken = default)
    {
        return await Companies
            .AsNoTracking()
            .SingleOrDefaultAsync(company => company.ExternalCode == externalCode, cancellationToken);
    }

    public async Task<IReadOnlyList<Company>> ListCompaniesAsync(CancellationToken cancellationToken = default)
    {
        return await Companies
            .AsNoTracking()
            .OrderBy(company => company.Name)
            .ThenBy(company => company.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> CompanyExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Companies
            .AsNoTracking()
            .AnyAsync(company => company.Id == id, cancellationToken);
    }

    public async Task AddCompanyAsync(Company company, CancellationToken cancellationToken = default)
    {
        await Companies.AddAsync(company, cancellationToken);
    }

    public async Task AddCompanyAddressAsync(CompanyAddress address, CancellationToken cancellationToken = default)
    {
        await CompanyAddresses.AddAsync(address, cancellationToken);
    }

    public async Task<IReadOnlyList<CompanyAddress>> ListCompanyAddressesAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        return await CompanyAddresses
            .AsNoTracking()
            .Where(address => address.CompanyId == companyId)
            .OrderBy(address => address.AddressType)
            .ThenBy(address => address.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<CompanyAddress?> GetCompanyAddressAsync(
        Guid companyId,
        Guid addressId,
        CancellationToken cancellationToken = default)
    {
        return await CompanyAddresses
            .AsNoTracking()
            .SingleOrDefaultAsync(
                address => address.CompanyId == companyId && address.Id == addressId,
                cancellationToken);
    }

    public async Task<CompanySaveChangesResult> SaveChangesAsyncHandlingDuplicateExternalCodeAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            await SaveChangesAsync(cancellationToken);
            return CompanySaveChangesResult.Saved;
        }
        catch (DbUpdateException exception) when (IsExternalCodeUniqueIndexViolation(exception))
        {
            return CompanySaveChangesResult.DuplicateExternalCode;
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CompanyDbContext).Assembly);
    }

    private static bool IsExternalCodeUniqueIndexViolation(DbUpdateException exception)
    {
        return exception.ToString().Contains(ExternalCodeIndexName, StringComparison.OrdinalIgnoreCase);
    }
}
