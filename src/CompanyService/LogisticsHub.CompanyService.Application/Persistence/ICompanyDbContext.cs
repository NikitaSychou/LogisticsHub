using LogisticsHub.CompanyService.Domain.Entities;

namespace LogisticsHub.CompanyService.Application.Persistence;

public interface ICompanyDbContext
{
    Task<Company?> GetCompanyByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Company?> GetCompanyForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Company?> GetCompanyByExternalCodeAsync(string externalCode, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Company>> ListCompaniesAsync(CancellationToken cancellationToken = default);

    Task<bool> CompanyExistsAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddCompanyAsync(Company company, CancellationToken cancellationToken = default);

    Task AddCompanyAddressAsync(CompanyAddress address, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CompanyAddress>> ListCompanyAddressesAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task<CompanyAddress?> GetCompanyAddressAsync(
        Guid companyId,
        Guid addressId,
        CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<CompanySaveChangesResult> SaveChangesAsyncHandlingDuplicateExternalCodeAsync(
        CancellationToken cancellationToken = default);
}
