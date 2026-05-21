using LogisticsHub.IntegrationEvents.StockReservations;
using LogisticsHub.Messaging.RabbitMQ;
using LogisticsHub.ShipmentService.Application.Shipments;
using Microsoft.Extensions.DependencyInjection;

namespace LogisticsHub.ShipmentService.Consumers;

public sealed class StockReservedConsumer
    : RabbitMqConsumerBackgroundService<StockReservedIntegrationEvent>
{
    private const string QueueName = "shipment.stock-reservation.reserved";

    private readonly IServiceScopeFactory serviceScopeFactory;

    public StockReservedConsumer(
        IRabbitMqConnectionProvider connectionProvider,
        RabbitMqOptions options,
        ILogger<StockReservedConsumer> logger,
        IServiceScopeFactory serviceScopeFactory)
        : base(
            connectionProvider,
            options,
            logger,
            QueueName,
            StockReservationRoutingKeys.Reserved)
    {
        this.serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task HandleMessageAsync(
        StockReservedIntegrationEvent message,
        CancellationToken cancellationToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var markShipmentReserved = scope.ServiceProvider.GetRequiredService<MarkShipmentReserved>();

        await markShipmentReserved.ExecuteAsync(
            message.EventId,
            message.ShipmentId,
            message.ReservationId,
            cancellationToken);
    }
}
