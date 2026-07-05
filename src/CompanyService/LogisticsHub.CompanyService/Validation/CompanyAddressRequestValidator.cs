using FluentValidation;
using LogisticsHub.CompanyService.Contracts;
using LogisticsHub.CompanyService.Domain.Enums;
using LogisticsHub.CompanyService.Localization;
using Microsoft.Extensions.Localization;

namespace LogisticsHub.CompanyService.Validation;

public sealed class CompanyAddressRequestValidator : AbstractValidator<CreateCompanyAddressRequest>
{
    public CompanyAddressRequestValidator(IStringLocalizer<CompanyValidationMessages> localizer)
    {
        RuleFor(request => request.AddressType)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage(_ => localizer["company_address.address_type.required"].Value)
            .Must(value => Enum.TryParse<CompanyAddressType>(value!.Trim(), ignoreCase: true, out _))
            .WithMessage(_ => localizer["company_address.address_type.invalid"].Value)
            .OverridePropertyName("addressType");

        RuleFor(request => request.CountryCode)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage(_ => localizer["company_address.country_code.required"].Value)
            .Must(value => value!.Trim().Length == 2)
            .WithMessage(_ => localizer["company_address.country_code.exact_length"].Value)
            .OverridePropertyName("countryCode");

        RuleFor(request => request.City)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage(_ => localizer["company_address.city.required"].Value)
            .Must(value => value!.Trim().Length <= 100)
            .WithMessage(_ => localizer["company_address.city.max_length"].Value)
            .OverridePropertyName("city");

        RuleFor(request => request.PostalCode)
            .Must(value => value!.Trim().Length <= 32)
            .When(request => !string.IsNullOrWhiteSpace(request.PostalCode))
            .WithMessage(_ => localizer["company_address.postal_code.max_length"].Value)
            .OverridePropertyName("postalCode");

        RuleFor(request => request.Line1)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage(_ => localizer["company_address.line1.required"].Value)
            .Must(value => value!.Trim().Length <= 200)
            .WithMessage(_ => localizer["company_address.line1.max_length"].Value)
            .OverridePropertyName("line1");

        RuleFor(request => request.Line2)
            .Must(value => value!.Trim().Length <= 200)
            .When(request => !string.IsNullOrWhiteSpace(request.Line2))
            .WithMessage(_ => localizer["company_address.line2.max_length"].Value)
            .OverridePropertyName("line2");
    }
}
