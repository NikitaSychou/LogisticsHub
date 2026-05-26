using LogisticsHub.ShipmentService.Application.Companies;

namespace LogisticsHub.ShipmentService.Application.Tests.Fakes;

public sealed class FakeCompanyAddressReferenceClient : ICompanyAddressReferenceClient
{
    private readonly Dictionary<(Guid CompanyId, Guid AddressId), CompanyAddressReferenceValidationResult> _results = [];

    public List<(Guid CompanyId, Guid AddressId)> Requests { get; } = [];

    public CompanyAddressReferenceValidationResult DefaultResult { get; set; } =
        CompanyAddressReferenceValidationResult.Found;

    public void SetResult(
        Guid companyId,
        Guid addressId,
        CompanyAddressReferenceValidationResult result)
    {
        _results[(companyId, addressId)] = result;
    }

    public Task<CompanyAddressReferenceValidationResult> ValidateAddressAsync(
        Guid companyId,
        Guid addressId,
        CancellationToken cancellationToken = default)
    {
        Requests.Add((companyId, addressId));

        return Task.FromResult(
            _results.TryGetValue((companyId, addressId), out var result)
                ? result
                : DefaultResult);
    }
}
