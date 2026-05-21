using LogisticsHub.IntegrationEvents.StockReservations;
using LogisticsHub.Messaging.RabbitMQ;
using LogisticsHub.ShipmentService.Application.Shipments;
using Microsoft.Extensions.DependencyInjection;

namespace LogisticsHub.ShipmentService.Consumers;

public sealed class StockReservationFailedConsumer
    : RabbitMqConsumerBackgroundService<StockReservationFailedIntegrationEvent>
{
    private const string QueueName = "shipment.stock-reservation.failed";

    private readonly IServiceScopeFactory serviceScopeFactory;

    public StockReservationFailedConsumer(
        IRabbitMqConnectionProvider connectionProvider,
        RabbitMqOptions options,
        ILogger<StockReservationFailedConsumer> logger,
        IServiceScopeFactory serviceScopeFactory)
        : base(
            connectionProvider,
            options,
            logger,
            QueueName,
            StockReservationRoutingKeys.Failed)
    {
        this.serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task HandleMessageAsync(
        StockReservationFailedIntegrationEvent message,
        CancellationToken cancellationToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var markShipmentReservationFailed = scope.ServiceProvider.GetRequiredService<MarkShipmentReservationFailed>();

        await markShipmentReservationFailed.ExecuteAsync(
            message.ShipmentId,
            message.Reason,
            cancellationToken);
    }
}
