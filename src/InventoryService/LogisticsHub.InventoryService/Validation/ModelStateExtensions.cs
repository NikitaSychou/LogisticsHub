using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace LogisticsHub.InventoryService.Validation;

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
}
