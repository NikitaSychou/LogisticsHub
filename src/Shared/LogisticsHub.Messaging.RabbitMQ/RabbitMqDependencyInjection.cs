using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LogisticsHub.Messaging.RabbitMQ;

public static class RabbitMqDependencyInjection
{
    public static IServiceCollection AddRabbitMqMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = new RabbitMqOptions();
        var section = configuration.GetSection("RabbitMq");
        section.Bind(options);

        ValidateOptions(section, options);

        services.AddSingleton(options);
        services.AddSingleton<IRabbitMqConnectionProvider, RabbitMqConnectionProvider>();
        services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();

        return services;
    }

    public static IHealthChecksBuilder AddRabbitMqHealthCheck(
        this IHealthChecksBuilder builder)
    {
        return builder.AddCheck<RabbitMqHealthCheck>("rabbitmq");
    }

    private static void ValidateOptions(
        IConfigurationSection section,
        RabbitMqOptions options)
    {
        if (!section.Exists())
        {
            throw new InvalidOperationException("RabbitMQ configuration section 'RabbitMq' is missing.");
        }

        ValidateRequired(section, nameof(RabbitMqOptions.HostName));
        ValidateRequired(section, nameof(RabbitMqOptions.UserName));
        ValidateRequired(section, nameof(RabbitMqOptions.Password));
        ValidateRequired(section, nameof(RabbitMqOptions.ExchangeName));

        if (options.Port <= 0 || options.Port > 65535)
        {
            throw new InvalidOperationException("RabbitMQ configuration value 'RabbitMq:Port' must be between 1 and 65535.");
        }
    }

    private static void ValidateRequired(
        IConfigurationSection section,
        string key)
    {
        if (string.IsNullOrWhiteSpace(section[key]))
        {
            throw new InvalidOperationException($"RabbitMQ configuration value 'RabbitMq:{key}' is required.");
        }
    }
}
