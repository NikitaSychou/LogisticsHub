using LogisticsHub.CompanyService.Application.Companies.Results;

namespace LogisticsHub.CompanyService.Application.Caching;

public interface ICompanyCache
{
    Task<CompanyResult?> GetOrCreateAsync(
        Guid companyId,
        Func<CancellationToken, Task<CompanyResult?>> sourceFactory,
        CancellationToken cancellationToken = default);

    Task InvalidateAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);
}
