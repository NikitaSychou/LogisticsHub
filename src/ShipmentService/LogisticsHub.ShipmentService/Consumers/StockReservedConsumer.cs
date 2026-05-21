using LogisticsHub.IntegrationEvents.StockReservations;
using LogisticsHub.Messaging.RabbitMQ;
using LogisticsHub.ShipmentService.Application.Shipments;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace LogisticsHub.ShipmentService.Consumers;

public sealed class StockReservedConsumer
    : RabbitMqConsumerBackgroundService<StockReservedIntegrationEvent>
{
    private const string QueueName = "shipment.stock-reservation.reserved";

    private readonly IServiceScopeFactory _serviceScopeFactory;

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
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task HandleMessageAsync(
        StockReservedIntegrationEvent message,
        CancellationToken cancellationToken)
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await mediator.Send(
            new MarkShipmentReservedCommand(message.EventId, message.ShipmentId, message.ReservationId),
            cancellationToken);
    }
}
