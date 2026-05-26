namespace LogisticsHub.ShipmentService.Application.Companies;

public interface ICompanyAddressReferenceClient
{
    Task<CompanyAddressReferenceValidationResult> ValidateAddressAsync(
        Guid companyId,
        Guid addressId,
        CancellationToken cancellationToken = default);
}
