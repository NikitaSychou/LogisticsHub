using LogisticsHub.IntegrationEvents.StockReservations;
using LogisticsHub.InventoryService.Application.StockReservations;
using LogisticsHub.Messaging.RabbitMQ;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace LogisticsHub.InventoryService.Consumers;

public sealed class StockReservationRequestedConsumer
    : RabbitMqConsumerBackgroundService<StockReservationRequestedIntegrationEvent>
{
    private const string QueueName = "inventory.stock-reservation.requested";

    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<StockReservationRequestedConsumer> _logger;

    public StockReservationRequestedConsumer(
        IRabbitMqConnectionProvider connectionProvider,
        RabbitMqOptions options,
        ILogger<StockReservationRequestedConsumer> logger,
        IServiceScopeFactory serviceScopeFactory)
        : base(
            connectionProvider,
            options,
            logger,
            QueueName,
            StockReservationRoutingKeys.Requested)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task HandleMessageAsync(
        StockReservationRequestedIntegrationEvent message,
        CancellationToken cancellationToken)
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();

        if (message.EventId == Guid.Empty)
        {
            _logger.LogWarning(
                "Received stock reservation request event with empty EventId for shipment {ShipmentId}. Failure result will be written to the inventory outbox without an inbox row.",
                message.ShipmentId);
        }

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var command = new CreateStockReservationCommand(
            message.ShipmentId,
            message.Items
                .Select(item => new StockReservationItemCommand(item.Sku, item.Quantity))
                .ToArray(),
            message.EventId);

        var result = await mediator.Send(command, cancellationToken);

        if (result.AlreadyProcessed)
        {
            _logger.LogInformation(
                "Ignoring duplicate stock reservation request event {EventId} for shipment {ShipmentId}.",
                message.EventId,
                message.ShipmentId);

            return;
        }
    }
}
