using LogisticsHub.InventoryService.Application.StockReservations;
using LogisticsHub.InventoryService.Contracts;
using LogisticsHub.InventoryService.Validation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsHub.InventoryService.Controllers;

[ApiController]
[Route("stock-reservations")]
public sealed class StockReservationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public StockReservationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{reservationId:guid}")]
    public async Task<IActionResult> GetAsync(
        Guid reservationId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetStockReservationQuery(reservationId), cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(ToGetResponse(result));
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        CreateStockReservationRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = CreateStockReservationRequestValidator.Validate(request);
        ModelState.AddValidationErrors(validationErrors);

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var command = new CreateStockReservationCommand(
            request.ShipmentId,
            request.Items!
                .Select(item => new StockReservationItemCommand(item.Sku.Trim(), item.Quantity))
                .ToArray());

        var result = await _mediator.Send(command, cancellationToken);

        if (result.Reservation is null)
        {
            return Conflict(new { reason = result.FailureReason });
        }

        var response = ToCreateResponse(result.Reservation);

        return Created($"/stock-reservations/{result.Reservation.ReservationId}", response);
    }

    private static CreateStockReservationResponse ToCreateResponse(StockReservationResult result)
    {
        return new CreateStockReservationResponse(
            result.ReservationId,
            result.ShipmentId,
            result.Status,
            ToItemResponses(result.Items));
    }

    private static GetStockReservationResponse ToGetResponse(StockReservationResult result)
    {
        return new GetStockReservationResponse(
            result.ReservationId,
            result.ShipmentId,
            result.Status,
            ToItemResponses(result.Items));
    }

    private static StockReservationItemResponse[] ToItemResponses(
        IReadOnlyCollection<StockReservationItemResult> items)
    {
        return items
            .Select(item => new StockReservationItemResponse(item.Sku, item.Quantity))
            .ToArray();
    }
}
