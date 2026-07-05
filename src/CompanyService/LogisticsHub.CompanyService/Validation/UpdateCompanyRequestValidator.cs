using FluentValidation;
using LogisticsHub.CompanyService.Contracts;
using LogisticsHub.CompanyService.Domain.Enums;

namespace LogisticsHub.CompanyService.Validation;

public sealed class UpdateCompanyRequestValidator : AbstractValidator<UpdateCompanyRequest>
{
    public UpdateCompanyRequestValidator()
    {
        RuleFor(request => request.Name)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Name is required.")
            .Must(value => value!.Trim().Length <= 200)
            .WithMessage("Name must be 200 characters or fewer.")
            .OverridePropertyName("name");

        RuleFor(request => request.ExternalCode)
            .Must(value => value!.Trim().Length <= 64)
            .When(request => !string.IsNullOrWhiteSpace(request.ExternalCode))
            .WithMessage("External code must be 64 characters or fewer.")
            .OverridePropertyName("externalCode");

        RuleFor(request => request.Status)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Status is required.")
            .Must(value => Enum.TryParse<CompanyStatus>(value!.Trim(), ignoreCase: true, out _))
            .WithMessage("Status must be Active or Inactive.")
            .OverridePropertyName("status");
    }
}
