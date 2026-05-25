using LogisticsHub.CompanyService.Application.Companies;
using LogisticsHub.CompanyService.Contracts;
using LogisticsHub.CompanyService.Domain.Enums;
using Riok.Mapperly.Abstractions;

namespace LogisticsHub.CompanyService.Mapping;

[Mapper]
public static partial class CompanyMapper
{
    public static CreateCompanyCommand ToCommand(CreateCompanyRequest request)
    {
        return new CreateCompanyCommand(
            request.Name!.Trim(),
            NormalizeOptional(request.ExternalCode),
            ParseCompanyStatus(request.Status));
    }

    public static UpdateCompanyCommand ToCommand(Guid id, UpdateCompanyRequest request)
    {
        return new UpdateCompanyCommand(
            id,
            request.Name!.Trim(),
            NormalizeOptional(request.ExternalCode),
            ParseCompanyStatus(request.Status));
    }

    public static CreateCompanyAddressCommand ToCommand(
        Guid companyId,
        CreateCompanyAddressRequest request)
    {
        return new CreateCompanyAddressCommand(
            companyId,
            ParseCompanyAddressType(request.AddressType),
            request.CountryCode!.Trim().ToUpperInvariant(),
            request.City!.Trim(),
            NormalizeOptional(request.PostalCode),
            request.Line1!.Trim(),
            NormalizeOptional(request.Line2));
    }

    public static CompanyResponse ToResponse(CompanyResult result)
    {
        return new CompanyResponse(
            result.Id,
            result.Name,
            result.ExternalCode,
            result.Status,
            result.CreatedAtUtc,
            result.UpdatedAtUtc);
    }

    public static CompanyAddressResponse ToResponse(CompanyAddressResult result)
    {
        return new CompanyAddressResponse(
            result.Id,
            result.CompanyId,
            result.AddressType,
            result.CountryCode,
            result.City,
            result.PostalCode,
            result.Line1,
            result.Line2,
            result.CreatedAtUtc,
            result.UpdatedAtUtc);
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static CompanyStatus ParseCompanyStatus(string? status)
    {
        return Enum.Parse<CompanyStatus>(status!.Trim(), ignoreCase: true);
    }

    private static CompanyAddressType ParseCompanyAddressType(string? addressType)
    {
        return Enum.Parse<CompanyAddressType>(addressType!.Trim(), ignoreCase: true);
    }
}
