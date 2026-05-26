using LogisticsHub.CompanyService.Application.Companies;

namespace LogisticsHub.CompanyService.Tests.Fakes;

public sealed class FakeCompanyAddressCache : ICompanyAddressCache
{
    private readonly Dictionary<(Guid CompanyId, Guid AddressId), CompanyAddressResult> _addresses = [];

    public int GetCallCount { get; private set; }

    public int SetCallCount { get; private set; }

    public bool FailOnGet { get; set; }

    public bool FailOnSet { get; set; }

    public void Add(CompanyAddressResult address)
    {
        _addresses[(address.CompanyId, address.Id)] = address;
    }

    public Task<CompanyAddressResult?> GetAsync(
        Guid companyId,
        Guid addressId,
        CancellationToken cancellationToken = default)
    {
        GetCallCount++;

        if (FailOnGet)
        {
            return Task.FromResult<CompanyAddressResult?>(null);
        }

        return Task.FromResult(
            _addresses.TryGetValue((companyId, addressId), out var address)
                ? address
                : null);
    }

    public Task SetAsync(
        CompanyAddressResult address,
        CancellationToken cancellationToken = default)
    {
        SetCallCount++;

        if (FailOnSet)
        {
            return Task.CompletedTask;
        }

        Add(address);
        return Task.CompletedTask;
    }
}
