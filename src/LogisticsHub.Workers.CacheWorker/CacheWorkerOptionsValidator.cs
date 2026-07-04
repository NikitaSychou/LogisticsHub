using Microsoft.Extensions.Options;

namespace LogisticsHub.Workers.CacheWorker;

public sealed class CacheWorkerOptionsValidator : IValidateOptions<CacheWorkerOptions>
{
    public ValidateOptionsResult Validate(string? name, CacheWorkerOptions options)
    {
        var failures = new List<string>();

        if (options.RefreshInterval <= TimeSpan.Zero)
        {
            failures.Add("CacheWorker:RefreshInterval must be greater than zero.");
        }

        if (options.GlobalTimeout <= TimeSpan.Zero)
        {
            failures.Add("CacheWorker:GlobalTimeout must be greater than zero.");
        }

        if (options.MaxDegreeOfParallelism <= 0)
        {
            failures.Add("CacheWorker:MaxDegreeOfParallelism must be greater than zero.");
        }

        AddJitterValidationFailure(
            failures,
            options.StartupJitterPercentage,
            "CacheWorker:StartupJitterPercentage");
        AddJitterValidationFailure(
            failures,
            options.RefreshJitterPercentage,
            "CacheWorker:RefreshJitterPercentage");

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void AddJitterValidationFailure(
        List<string> failures,
        double jitterPercentage,
        string optionName)
    {
        if (double.IsNaN(jitterPercentage) ||
            double.IsInfinity(jitterPercentage) ||
            jitterPercentage < 0 ||
            jitterPercentage > CacheWorkerOptions.MaximumJitterPercentage)
        {
            failures.Add(
                $"{optionName} must be between 0 and {CacheWorkerOptions.MaximumJitterPercentage}.");
        }
    }
}
