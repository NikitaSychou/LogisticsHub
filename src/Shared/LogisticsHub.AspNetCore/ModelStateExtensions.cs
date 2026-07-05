using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace LogisticsHub.AspNetCore;

public static class ModelStateExtensions
{
    public static void AddValidationErrors(
        this ModelStateDictionary modelState,
        Dictionary<string, string[]> validationErrors)
    {
        foreach (var validationError in validationErrors)
        {
            foreach (var message in validationError.Value)
            {
                modelState.AddModelError(validationError.Key, message);
            }
        }
    }

    public static void AddValidationErrors(
        this ModelStateDictionary modelState,
        ValidationResult validationResult)
    {
        foreach (var validationFailure in validationResult.Errors)
        {
            modelState.AddModelError(validationFailure.PropertyName, validationFailure.ErrorMessage);
        }
    }
}
