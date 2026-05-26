namespace LogisticsHub.CompanyService.Application.Companies;

public interface ICompanyAddressCache
{
    Task<CompanyAddressResult?> GetAsync(
        Guid companyId,
        Guid addressId,
        CancellationToken cancellationToken = default);

    Task SetAsync(
        CompanyAddressResult address,
        CancellationToken cancellationToken = default);
}
