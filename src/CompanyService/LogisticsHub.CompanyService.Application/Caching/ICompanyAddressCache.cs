using LogisticsHub.CompanyService.Application.Companies;

namespace LogisticsHub.CompanyService.Application.Caching;

public interface ICompanyAddressCache
{
    Task<CompanyAddressResult?> GetOrCreateAsync(
        Guid companyId,
        Guid addressId,
        Func<CancellationToken, Task<CompanyAddressResult?>> sourceFactory,
        CancellationToken cancellationToken = default);
}
