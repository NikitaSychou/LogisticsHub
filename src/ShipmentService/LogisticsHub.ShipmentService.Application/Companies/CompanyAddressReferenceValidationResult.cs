namespace LogisticsHub.ShipmentService.Application.Companies;

public sealed record CompanyAddressReferenceValidationResult(
    CompanyAddressReferenceValidationStatus Status)
{
    public static CompanyAddressReferenceValidationResult Found { get; } =
        new(CompanyAddressReferenceValidationStatus.Found);

    public static CompanyAddressReferenceValidationResult NotFound { get; } =
        new(CompanyAddressReferenceValidationStatus.NotFound);

    public static CompanyAddressReferenceValidationResult DependencyUnavailable { get; } =
        new(CompanyAddressReferenceValidationStatus.DependencyUnavailable);
}

public enum CompanyAddressReferenceValidationStatus
{
    Found,
    NotFound,
    DependencyUnavailable
}
