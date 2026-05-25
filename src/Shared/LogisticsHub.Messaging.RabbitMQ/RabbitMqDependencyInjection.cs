using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace LogisticsHub.Messaging.RabbitMQ;

public static class RabbitMqDependencyInjection
{
    public static IServiceCollection AddRabbitMqMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection("RabbitMq");

        if (!section.Exists())
        {
            throw new InvalidOperationException("RabbitMQ configuration section 'RabbitMq' is missing.");
        }

        services
            .AddOptions<RabbitMqOptions>()
            .Configure(section.Bind)
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.HostName),
                "RabbitMQ configuration value 'RabbitMq:HostName' is required.")
            .Validate(
                options => options.Port is >= 1 and <= 65535,
                "RabbitMQ configuration value 'RabbitMq:Port' must be between 1 and 65535.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.UserName),
                "RabbitMQ configuration value 'RabbitMq:UserName' is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Password),
                "RabbitMQ configuration value 'RabbitMq:Password' is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ExchangeName),
                "RabbitMQ configuration value 'RabbitMq:ExchangeName' is required.")
            .ValidateOnStart();

        services.AddSingleton(serviceProvider =>
            serviceProvider.GetRequiredService<IOptions<RabbitMqOptions>>().Value);
        services.AddSingleton<IRabbitMqConnectionProvider, RabbitMqConnectionProvider>();
        services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();

        return services;
    }

    public static IHealthChecksBuilder AddRabbitMqHealthCheck(
        this IHealthChecksBuilder builder)
    {
        return builder.AddCheck<RabbitMqHealthCheck>("rabbitmq");
    }
}
