using System.Text.Json;
using System.Text.Json.Serialization;

namespace LogisticsHub.Caching;

public sealed class CacheOptions
{
    public const double MaximumTtlJitterPercentage = 50;

    public TimeSpan DefaultTtl { get; set; } = TimeSpan.FromHours(24);

    public double TtlJitterPercentage { get; set; } = 5;

    public JsonSerializerOptions JsonSerializerOptions { get; } = CreateDefaultJsonSerializerOptions();

    internal void Validate()
    {
        if (DefaultTtl <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Cache default TTL must be greater than zero.");
        }

        if (double.IsNaN(TtlJitterPercentage) ||
            double.IsInfinity(TtlJitterPercentage) ||
            TtlJitterPercentage < 0 ||
            TtlJitterPercentage > MaximumTtlJitterPercentage)
        {
            throw new InvalidOperationException(
                $"Cache TTL jitter percentage must be between 0 and {MaximumTtlJitterPercentage}.");
        }
    }

    private static JsonSerializerOptions CreateDefaultJsonSerializerOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());

        return options;
    }
}
