using LogisticsHub.CompanyService.Application.Persistence;
using LogisticsHub.CompanyService.Domain.Entities;

namespace LogisticsHub.CompanyService.Tests.Fakes;

public sealed class FakeCompanyDbContext : ICompanyDbContext
{
    public List<Company> Companies { get; } = [];
    public List<CompanyAddress> CompanyAddresses { get; } = [];

    public CompanySaveChangesResult SaveChangesResult { get; set; } = CompanySaveChangesResult.Saved;

    public Task<Company?> GetCompanyByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Companies.SingleOrDefault(company => company.Id == id));
    }

    public Task<Company?> GetCompanyForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Companies.SingleOrDefault(company => company.Id == id));
    }

    public Task<Company?> GetCompanyByExternalCodeAsync(
        string externalCode,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Companies.SingleOrDefault(company => company.ExternalCode == externalCode));
    }

    public Task<IReadOnlyList<Company>> ListCompaniesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Company>>(Companies.ToArray());
    }

    public Task<bool> CompanyExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Companies.Any(company => company.Id == id));
    }

    public Task AddCompanyAsync(Company company, CancellationToken cancellationToken = default)
    {
        Companies.Add(company);
        return Task.CompletedTask;
    }

    public Task AddCompanyAddressAsync(CompanyAddress address, CancellationToken cancellationToken = default)
    {
        CompanyAddresses.Add(address);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<CompanyAddress>> ListCompanyAddressesAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var addresses = CompanyAddresses
            .Where(address => address.CompanyId == companyId)
            .ToArray();

        return Task.FromResult<IReadOnlyList<CompanyAddress>>(addresses);
    }

    public Task<CompanyAddress?> GetCompanyAddressAsync(
        Guid companyId,
        Guid addressId,
        CancellationToken cancellationToken = default)
    {
        var address = CompanyAddresses
            .SingleOrDefault(address => address.CompanyId == companyId && address.Id == addressId);

        return Task.FromResult(address);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(1);
    }

    public Task<CompanySaveChangesResult> SaveChangesAsyncHandlingDuplicateExternalCodeAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(SaveChangesResult);
    }
}
