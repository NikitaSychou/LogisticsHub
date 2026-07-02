using LogisticsHub.CompanyService.Application.Persistence;
using LogisticsHub.CompanyService.Domain.Entities;

namespace LogisticsHub.CompanyService.Tests.Fakes;

public sealed class FakeCompanyDbContext : ICompanyDbContext
{
    public List<Company> Companies { get; } = [];
    public List<CompanyAddress> CompanyAddresses { get; } = [];
    public List<int> CompanyBatchCountsAtSave { get; } = [];
    public List<int> SavedAddressBatchCounts { get; } = [];
    public List<int> TrackedEntityCountsAtSave { get; } = [];

    public CompanySaveChangesResult SaveChangesResult { get; set; } = CompanySaveChangesResult.Saved;

    public bool ThrowOnSaveChanges { get; set; }

    public int? FailOnSaveChangesCall { get; set; }

    public int GetCompanyByIdCallCount { get; private set; }

    public int SaveChangesCallCount { get; private set; }

    public int ClearChangeTrackerCallCount { get; private set; }

    public int CurrentTrackedCompanyCount { get; private set; }

    public int CurrentTrackedAddressCount { get; private set; }

    public CancellationToken LastGetCompanyByIdCancellationToken { get; private set; }

    public CancellationToken LastSaveChangesCancellationToken { get; private set; }

    public Task<Company?> GetCompanyByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        GetCompanyByIdCallCount++;
        LastGetCompanyByIdCancellationToken = cancellationToken;

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
        CurrentTrackedCompanyCount++;
        CurrentTrackedAddressCount += company.Addresses.Count;
        return Task.CompletedTask;
    }

    public Task AddCompanyAddressAsync(CompanyAddress address, CancellationToken cancellationToken = default)
    {
        CompanyAddresses.Add(address);
        CurrentTrackedAddressCount++;
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
        cancellationToken.ThrowIfCancellationRequested();

        SaveChangesCallCount++;
        LastSaveChangesCancellationToken = cancellationToken;
        CompanyBatchCountsAtSave.Add(CurrentTrackedCompanyCount);
        SavedAddressBatchCounts.Add(CurrentTrackedAddressCount);
        TrackedEntityCountsAtSave.Add(CurrentTrackedCompanyCount + CurrentTrackedAddressCount);

        if (FailOnSaveChangesCall == SaveChangesCallCount)
        {
            throw new InvalidOperationException("save failed");
        }

        return Task.FromResult(1);
    }

    public Task<CompanySaveChangesResult> SaveChangesAsyncHandlingDuplicateExternalCodeAsync(
        CancellationToken cancellationToken = default)
    {
        if (ThrowOnSaveChanges)
        {
            throw new InvalidOperationException("save failed");
        }

        return Task.FromResult(SaveChangesResult);
    }

    public void ClearChangeTracker()
    {
        ClearChangeTrackerCallCount++;
        CurrentTrackedCompanyCount = 0;
        CurrentTrackedAddressCount = 0;
    }
}
