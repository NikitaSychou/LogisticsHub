using LogisticsHub.ShipmentService.Application.Companies;

namespace LogisticsHub.ShipmentService.Infrastructure.Companies;

public sealed class CompanyServiceClient : ICompanyAddressReferenceClient
{
    private readonly HttpClient _httpClient;

    public CompanyServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CompanyAddressReferenceValidationResult> ValidateAddressAsync(
        Guid companyId,
        Guid addressId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync(
                $"companies/{companyId:D}/addresses/{addressId:D}",
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return CompanyAddressReferenceValidationResult.Found;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return CompanyAddressReferenceValidationResult.NotFound;
            }

            return CompanyAddressReferenceValidationResult.DependencyUnavailable;
        }
        catch (HttpRequestException)
        {
            return CompanyAddressReferenceValidationResult.DependencyUnavailable;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return CompanyAddressReferenceValidationResult.DependencyUnavailable;
        }
    }
}
