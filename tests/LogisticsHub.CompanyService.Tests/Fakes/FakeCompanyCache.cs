using LogisticsHub.CompanyService.Application.Caching;
using LogisticsHub.CompanyService.Application.Companies;

namespace LogisticsHub.CompanyService.Tests.Fakes;

public sealed class FakeCompanyCache : ICompanyCache
{
    private readonly Dictionary<Guid, CompanyResult> _companies = [];

    public int GetOrCreateCallCount { get; private set; }

    public int InvalidateCallCount { get; private set; }

    public Guid? LastInvalidatedCompanyId { get; private set; }

    public CancellationToken LastInvalidateCancellationToken { get; private set; }

    public void Add(CompanyResult company)
    {
        _companies[company.Id] = company;
    }

    public async Task<CompanyResult?> GetOrCreateAsync(
        Guid companyId,
        Func<CancellationToken, Task<CompanyResult?>> sourceFactory,
        CancellationToken cancellationToken = default)
    {
        GetOrCreateCallCount++;

        if (_companies.TryGetValue(companyId, out var company))
        {
            return company;
        }

        var loadedCompany = await sourceFactory(cancellationToken);
        if (loadedCompany is not null)
        {
            Add(loadedCompany);
        }

        return loadedCompany;
    }

    public Task InvalidateAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        InvalidateCallCount++;
        LastInvalidatedCompanyId = companyId;
        LastInvalidateCancellationToken = cancellationToken;
        _companies.Remove(companyId);

        return Task.CompletedTask;
    }
}
