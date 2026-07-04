using LogisticsHub.CompanyService.Application.Companies;

namespace LogisticsHub.Workers.CacheWorker;

public interface ICompanyCacheWarmupReader
{
    Task<IReadOnlyList<CompanyResult>> ReadCompaniesAsync(
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CompanyAddressResult>> ReadCompanyAddressesAsync(
        int skip,
        int take,
        CancellationToken cancellationToken);
}
