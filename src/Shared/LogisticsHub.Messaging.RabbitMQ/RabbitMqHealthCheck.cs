using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

namespace LogisticsHub.Messaging.RabbitMQ;

public sealed class RabbitMqHealthCheck : IHealthCheck
{
    private readonly RabbitMqOptions _options;

    public RabbitMqHealthCheck(RabbitMqOptions options)
    {
        _options = options;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password
            };

            await using var connection = await factory.CreateConnectionAsync(cancellationToken);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

            await channel.ExchangeDeclarePassiveAsync(
                _options.ExchangeName,
                cancellationToken: cancellationToken);

            return HealthCheckResult.Healthy("RabbitMQ is available.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ is unavailable.", exception);
        }
    }
}
