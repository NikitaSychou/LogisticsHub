using System.Text.Json;
using System.Text.Json.Serialization;
using LogisticsHub.CompanyService.Application.Companies;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace LogisticsHub.CompanyService.Infrastructure.Caching;

public sealed class RedisCompanyAddressCache : ICompanyAddressCache
{
    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
    };

    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCompanyAddressCache> _logger;

    public RedisCompanyAddressCache(
        IDistributedCache cache,
        ILogger<RedisCompanyAddressCache> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<CompanyAddressResult?> GetAsync(
        Guid companyId,
        Guid addressId,
        CancellationToken cancellationToken = default)
    {
        var key = BuildKey(companyId, addressId);

        try
        {
            var json = await _cache.GetStringAsync(key, cancellationToken);
            return string.IsNullOrWhiteSpace(json)
                ? null
                : JsonSerializer.Deserialize<CompanyAddressResult>(json, JsonSerializerOptions);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(
                exception,
                "Failed to read company address cache entry for company {CompanyId} and address {AddressId}.",
                companyId,
                addressId);

            return null;
        }
    }

    public async Task SetAsync(
        CompanyAddressResult address,
        CancellationToken cancellationToken = default)
    {
        var key = BuildKey(address.CompanyId, address.Id);

        try
        {
            var json = JsonSerializer.Serialize(address, JsonSerializerOptions);
            await _cache.SetStringAsync(key, json, CacheOptions, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(
                exception,
                "Failed to write company address cache entry for company {CompanyId} and address {AddressId}.",
                address.CompanyId,
                address.Id);
        }
    }

    public static string BuildKey(Guid companyId, Guid addressId)
    {
        return $"company-address:{companyId:D}:{addressId:D}";
    }
}
