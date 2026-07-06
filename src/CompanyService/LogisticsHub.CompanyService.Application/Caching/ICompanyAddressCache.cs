using LogisticsHub.CompanyService.Application.Companies.Results;

namespace LogisticsHub.CompanyService.Application.Caching;

public interface ICompanyAddressCache
{
    Task<CompanyAddressResult?> GetOrCreateAsync(
        Guid companyId,
        Guid addressId,
        Func<CancellationToken, Task<CompanyAddressResult?>> sourceFactory,
        CancellationToken cancellationToken = default);

    Task InvalidateAsync(
        Guid companyId,
        Guid addressId,
        CancellationToken cancellationToken = default);
}
