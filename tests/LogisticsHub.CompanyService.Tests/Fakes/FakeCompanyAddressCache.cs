using LogisticsHub.CompanyService.Application.Caching;
using LogisticsHub.CompanyService.Application.Companies;

namespace LogisticsHub.CompanyService.Tests.Fakes;

public sealed class FakeCompanyAddressCache : ICompanyAddressCache
{
    private readonly Dictionary<(Guid CompanyId, Guid AddressId), CompanyAddressResult> _addresses = [];

    public int GetOrCreateCallCount { get; private set; }

    public bool FailOnGetOrCreate { get; set; }

    public void Add(CompanyAddressResult address)
    {
        _addresses[(address.CompanyId, address.Id)] = address;
    }

    public async Task<CompanyAddressResult?> GetOrCreateAsync(
        Guid companyId,
        Guid addressId,
        Func<CancellationToken, Task<CompanyAddressResult?>> sourceFactory,
        CancellationToken cancellationToken = default)
    {
        GetOrCreateCallCount++;

        if (FailOnGetOrCreate)
        {
            return await sourceFactory(cancellationToken);
        }

        if (_addresses.TryGetValue((companyId, addressId), out var address))
        {
            return address;
        }

        var loadedAddress = await sourceFactory(cancellationToken);
        if (loadedAddress is not null)
        {
            Add(loadedAddress);
        }

        return loadedAddress;
    }
}
