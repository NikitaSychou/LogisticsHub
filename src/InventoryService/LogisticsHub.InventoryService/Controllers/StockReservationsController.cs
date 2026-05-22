using LogisticsHub.AspNetCore;
using LogisticsHub.InventoryService.Application.StockReservations;
using LogisticsHub.InventoryService.Contracts;
using LogisticsHub.InventoryService.Mapping;
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
    [ProducesResponseType(typeof(GetStockReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsync(
        Guid reservationId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetStockReservationQuery(reservationId), cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(StockReservationMapper.ToGetResponse(result));
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateStockReservationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
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

        var command = StockReservationMapper.ToCommand(request);

        var result = await _mediator.Send(command, cancellationToken);

        if (result.Reservation is null)
        {
            return Conflict(new { reason = result.FailureReason });
        }

        var response = StockReservationMapper.ToCreateResponse(result.Reservation);

        return Created($"/stock-reservations/{result.Reservation.ReservationId}", response);
    }
}
