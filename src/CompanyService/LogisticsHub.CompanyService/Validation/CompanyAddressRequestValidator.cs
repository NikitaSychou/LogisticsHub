using FluentValidation;
using LogisticsHub.CompanyService.Contracts;
using LogisticsHub.CompanyService.Domain.Enums;

namespace LogisticsHub.CompanyService.Validation;

public sealed class CompanyAddressRequestValidator : AbstractValidator<CreateCompanyAddressRequest>
{
    public CompanyAddressRequestValidator()
    {
        RuleFor(request => request.AddressType)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Address type is required.")
            .Must(value => Enum.TryParse<CompanyAddressType>(value!.Trim(), ignoreCase: true, out _))
            .WithMessage("Address type must be Legal, Billing, Shipping, or Warehouse.")
            .OverridePropertyName("addressType");

        RuleFor(request => request.CountryCode)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Country code is required.")
            .Must(value => value!.Trim().Length == 2)
            .WithMessage("Country code must be exactly 2 characters.")
            .OverridePropertyName("countryCode");

        RuleFor(request => request.City)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("City is required.")
            .Must(value => value!.Trim().Length <= 100)
            .WithMessage("City must be 100 characters or fewer.")
            .OverridePropertyName("city");

        RuleFor(request => request.PostalCode)
            .Must(value => value!.Trim().Length <= 32)
            .When(request => !string.IsNullOrWhiteSpace(request.PostalCode))
            .WithMessage("Postal code must be 32 characters or fewer.")
            .OverridePropertyName("postalCode");

        RuleFor(request => request.Line1)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Line1 is required.")
            .Must(value => value!.Trim().Length <= 200)
            .WithMessage("Line1 must be 200 characters or fewer.")
            .OverridePropertyName("line1");

        RuleFor(request => request.Line2)
            .Must(value => value!.Trim().Length <= 200)
            .When(request => !string.IsNullOrWhiteSpace(request.Line2))
            .WithMessage("Line2 must be 200 characters or fewer.")
            .OverridePropertyName("line2");
    }
}
