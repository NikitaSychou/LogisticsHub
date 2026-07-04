using LogisticsHub.CompanyService.Application.Companies;
using LogisticsHub.CompanyService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsHub.Workers.CacheWorker;

public sealed class EfCompanyCacheWarmupReader : ICompanyCacheWarmupReader
{
    private readonly IDbContextFactory<CompanyDbContext> _dbContextFactory;

    public EfCompanyCacheWarmupReader(IDbContextFactory<CompanyDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<IReadOnlyList<CompanyResult>> ReadCompaniesAsync(
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var companies = await dbContext.Companies
            .AsNoTracking()
            .OrderBy(company => company.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return companies
            .Select(CompanyResultFactory.ToResult)
            .ToArray();
    }

    public async Task<IReadOnlyList<CompanyAddressResult>> ReadCompanyAddressesAsync(
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var addresses = await dbContext.CompanyAddresses
            .AsNoTracking()
            .OrderBy(address => address.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return addresses
            .Select(CompanyResultFactory.ToResult)
            .ToArray();
    }
}
