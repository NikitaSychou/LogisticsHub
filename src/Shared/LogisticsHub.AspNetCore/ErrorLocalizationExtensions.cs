using System.Globalization;
using LogisticsHub.Results;
using Microsoft.Extensions.Localization;

namespace LogisticsHub.AspNetCore;

public static class ErrorLocalizationExtensions
{
    public static string ToLocalizedMessage(this Error error, IStringLocalizer localizer)
    {
        var localized = localizer[error.Code];
        var template = localized.ResourceNotFound ? error.Description : localized.Value;

        return FormatMetadata(template, error.Metadata);
    }

    private static string FormatMetadata(
        string template,
        IReadOnlyDictionary<string, object?> metadata)
    {
        var message = template;

        foreach (var item in metadata)
        {
            var value = Convert.ToString(item.Value, CultureInfo.CurrentCulture) ?? string.Empty;
            message = message.Replace(
                "{" + item.Key + "}",
                value,
                StringComparison.Ordinal);
        }

        return message;
    }
}
