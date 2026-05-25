using LogisticsHub.CompanyService.Contracts;
using LogisticsHub.CompanyService.Domain.Enums;

namespace LogisticsHub.CompanyService.Validation;

public static class CompanyAddressRequestValidator
{
    public static Dictionary<string, string[]> Validate(CreateCompanyAddressRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.AddressType))
        {
            errors["addressType"] = ["Address type is required."];
        }
        else if (!Enum.TryParse<CompanyAddressType>(request.AddressType.Trim(), ignoreCase: true, out _))
        {
            errors["addressType"] = ["Address type must be Legal, Billing, Shipping, or Warehouse."];
        }

        if (string.IsNullOrWhiteSpace(request.CountryCode))
        {
            errors["countryCode"] = ["Country code is required."];
        }
        else if (request.CountryCode.Trim().Length != 2)
        {
            errors["countryCode"] = ["Country code must be exactly 2 characters."];
        }

        if (string.IsNullOrWhiteSpace(request.City))
        {
            errors["city"] = ["City is required."];
        }
        else if (request.City.Trim().Length > 100)
        {
            errors["city"] = ["City must be 100 characters or fewer."];
        }

        if (!string.IsNullOrWhiteSpace(request.PostalCode) && request.PostalCode.Trim().Length > 32)
        {
            errors["postalCode"] = ["Postal code must be 32 characters or fewer."];
        }

        if (string.IsNullOrWhiteSpace(request.Line1))
        {
            errors["line1"] = ["Line1 is required."];
        }
        else if (request.Line1.Trim().Length > 200)
        {
            errors["line1"] = ["Line1 must be 200 characters or fewer."];
        }

        if (!string.IsNullOrWhiteSpace(request.Line2) && request.Line2.Trim().Length > 200)
        {
            errors["line2"] = ["Line2 must be 200 characters or fewer."];
        }

        return errors;
    }
}
