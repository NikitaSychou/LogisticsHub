using System.Text.Json;
using System.Text.Json.Serialization;

namespace LogisticsHub.Caching;

public sealed class CacheOptions
{
    public TimeSpan DefaultTtl { get; set; } = TimeSpan.FromHours(24);

    public JsonSerializerOptions JsonSerializerOptions { get; } = CreateDefaultJsonSerializerOptions();

    private static JsonSerializerOptions CreateDefaultJsonSerializerOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());

        return options;
    }
}
