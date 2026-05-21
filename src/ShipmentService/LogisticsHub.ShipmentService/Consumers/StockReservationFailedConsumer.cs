using LogisticsHub.IntegrationEvents.StockReservations;
using LogisticsHub.Messaging.RabbitMQ;
using LogisticsHub.ShipmentService.Application.Shipments;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace LogisticsHub.ShipmentService.Consumers;

public sealed class StockReservationFailedConsumer
    : RabbitMqConsumerBackgroundService<StockReservationFailedIntegrationEvent>
{
    private const string QueueName = "shipment.stock-reservation.failed";

    private readonly IServiceScopeFactory _serviceScopeFactory;

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
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task HandleMessageAsync(
        StockReservationFailedIntegrationEvent message,
        CancellationToken cancellationToken)
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await mediator.Send(
            new MarkShipmentReservationFailedCommand(message.EventId, message.ShipmentId, message.Reason),
            cancellationToken);
    }
}
