using FluentValidation;
using LogisticsHub.CompanyService.Contracts;
using LogisticsHub.CompanyService.Domain.Enums;
using LogisticsHub.CompanyService.Localization;
using Microsoft.Extensions.Localization;

namespace LogisticsHub.CompanyService.Validation;

public sealed class CreateCompanyRequestValidator : AbstractValidator<CreateCompanyRequest>
{
    public CreateCompanyRequestValidator(IStringLocalizer<CompanyValidationMessages> localizer)
    {
        RuleFor(request => request.Name)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage(_ => localizer["company.name.required"].Value)
            .Must(value => value!.Trim().Length <= 200)
            .WithMessage(_ => localizer["company.name.max_length"].Value)
            .OverridePropertyName("name");

        RuleFor(request => request.ExternalCode)
            .Must(value => value!.Trim().Length <= 64)
            .When(request => !string.IsNullOrWhiteSpace(request.ExternalCode))
            .WithMessage(_ => localizer["company.external_code.max_length"].Value)
            .OverridePropertyName("externalCode");

        RuleFor(request => request.Status)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage(_ => localizer["company.status.required"].Value)
            .Must(value => Enum.TryParse<CompanyStatus>(value!.Trim(), ignoreCase: true, out _))
            .WithMessage(_ => localizer["company.status.invalid"].Value)
            .OverridePropertyName("status");
    }
}
