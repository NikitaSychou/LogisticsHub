using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LogisticsHub.Messaging.RabbitMQ;

public static class RabbitMqDependencyInjection
{
    public static IServiceCollection AddRabbitMqMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = new RabbitMqOptions();
        configuration.GetSection("RabbitMq").Bind(options);

        services.AddSingleton(options);
        services.AddSingleton<IRabbitMqConnectionProvider, RabbitMqConnectionProvider>();
        services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();

        return services;
    }
}
