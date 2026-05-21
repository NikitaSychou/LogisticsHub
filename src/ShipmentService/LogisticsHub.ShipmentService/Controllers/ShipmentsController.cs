using LogisticsHub.IntegrationEvents.StockReservations;
using LogisticsHub.Messaging.RabbitMQ;
using LogisticsHub.ShipmentService.Application.Shipments;
using LogisticsHub.ShipmentService.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsHub.ShipmentService.Controllers;

[ApiController]
[Route("shipments")]
public sealed class ShipmentsController : ControllerBase
{
    private readonly CreateShipment _createShipment;
    private readonly GetShipment _getShipment;
    private readonly IRabbitMqPublisher _rabbitMqPublisher;

    public ShipmentsController(
        CreateShipment createShipment,
        GetShipment getShipment,
        IRabbitMqPublisher rabbitMqPublisher)
    {
        _createShipment = createShipment;
        _getShipment = getShipment;
        _rabbitMqPublisher = rabbitMqPublisher;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _getShipment.ExecuteAsync(id, cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        var response = new GetShipmentResponse(
            result.ShipmentId,
            result.ShipmentNumber,
            result.Status,
            result.ReservationId,
            result.ReservationFailureReason,
            result.DestinationName,
            result.DestinationAddress,
            result.Comment,
            result.CreatedAt,
            result.UpdatedAt,
            result.DispatchedAt,
            result.CancelledAt,
            result.Items
                .Select(item => new GetShipmentItemResponse(item.Sku, item.Quantity))
                .ToArray());

        return Ok(response);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        CreateShipmentRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = CreateShipmentRequestValidator.Validate(request);
        if (validationErrors.Count > 0)
        {
            foreach (var validationError in validationErrors)
            {
                foreach (var message in validationError.Value)
                {
                    ModelState.AddModelError(validationError.Key, message);
                }
            }

            return ValidationProblem(ModelState);
        }

        var commandItems = request.Items
            .Select(item => new CreateShipmentItemCommand(item.Sku, item.Quantity))
            .ToArray();

        var command = new CreateShipmentCommand(commandItems);

        var result = await _createShipment.ExecuteAsync(command, cancellationToken);

        await _rabbitMqPublisher.PublishAsync(
            StockReservationRoutingKeys.Requested,
            new StockReservationRequestedIntegrationEvent(
                Guid.NewGuid(),
                DateTime.UtcNow,
                result.ShipmentId,
                commandItems
                    .Select(item => new StockReservationRequestedItem(item.Sku, item.Quantity))
                    .ToArray()),
            cancellationToken);

        return Created($"/shipments/{result.ShipmentId}", result);
    }
}
