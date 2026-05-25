using LogisticsHub.CompanyService.Contracts;
using LogisticsHub.CompanyService.Domain.Enums;

namespace LogisticsHub.CompanyService.Validation;

public static class CompanyRequestValidator
{
    public static Dictionary<string, string[]> Validate(CreateCompanyRequest request)
    {
        return ValidateCompany(request.Name, request.ExternalCode, request.Status);
    }

    public static Dictionary<string, string[]> Validate(UpdateCompanyRequest request)
    {
        return ValidateCompany(request.Name, request.ExternalCode, request.Status);
    }

    private static Dictionary<string, string[]> ValidateCompany(
        string? name,
        string? externalCode,
        string? status)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors["name"] = ["Name is required."];
        }
        else if (name.Trim().Length > 200)
        {
            errors["name"] = ["Name must be 200 characters or fewer."];
        }

        if (!string.IsNullOrWhiteSpace(externalCode) && externalCode.Trim().Length > 64)
        {
            errors["externalCode"] = ["External code must be 64 characters or fewer."];
        }

        if (string.IsNullOrWhiteSpace(status))
        {
            errors["status"] = ["Status is required."];
        }
        else if (!Enum.TryParse<CompanyStatus>(status.Trim(), ignoreCase: true, out _))
        {
            errors["status"] = ["Status must be Active or Inactive."];
        }

        return errors;
    }
}
