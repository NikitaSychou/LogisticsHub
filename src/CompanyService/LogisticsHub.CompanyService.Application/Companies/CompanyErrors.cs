using LogisticsHub.Results;

namespace LogisticsHub.CompanyService.Application.Companies;

public static class CompanyErrors
{
    public static Error NotFound(Guid id)
    {
        return new Error(
            "company.not_found",
            "Company was not found.",
            new Dictionary<string, object?> { ["id"] = id });
    }

    public static Error ExternalCodeAlreadyExists(string externalCode)
    {
        return new Error(
            "company.external_code_already_exists",
            "Company external code already exists.",
            new Dictionary<string, object?> { ["externalCode"] = externalCode });
    }

    public static Error AddressCompanyNotFound(Guid companyId)
    {
        return new Error(
            "company.address.company_not_found",
            "Company for address was not found.",
            new Dictionary<string, object?> { ["companyId"] = companyId });
    }
}
