using FluentValidation;
using LogisticsHub.AspNetCore;
using LogisticsHub.InventoryService.Application.StockReservations;
using LogisticsHub.InventoryService.Contracts;
using LogisticsHub.InventoryService.Localization;
using LogisticsHub.InventoryService.Mapping;
using LogisticsHub.InventoryService.Validation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace LogisticsHub.InventoryService.Controllers;

[ApiController]
[Route("stock-reservations")]
public sealed class StockReservationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IStringLocalizer<InventoryBusinessErrorMessages> _errorLocalizer;
    private readonly IValidator<CreateStockReservationRequest> _validator;

    public StockReservationsController(
        IMediator mediator,
        IStringLocalizer<InventoryBusinessErrorMessages> errorLocalizer,
        IValidator<CreateStockReservationRequest> validator)
    {
        _mediator = mediator;
        _errorLocalizer = errorLocalizer;
        _validator = validator;
    }

    [HttpGet("{reservationId:guid}")]
    [ProducesResponseType(typeof(GetStockReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsync(
        Guid reservationId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetStockReservationQuery(reservationId), cancellationToken);

        if (result.IsFailure)
        {
            return NotFound();
        }

        return Ok(StockReservationMapper.ToGetResponse(result.Value));
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateStockReservationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAsync(
        CreateStockReservationRequest request,
        CancellationToken cancellationToken)
    {
        var validationProblem = ValidateRequest(_validator.Validate(request));
        if (validationProblem is not null)
        {
            return validationProblem;
        }

        var command = StockReservationMapper.ToCommand(request);

        var result = await _mediator.Send(command, cancellationToken);

        if (result.Reservation is null)
        {
            return Conflict(new { reason = result.Error.ToLocalizedMessage(_errorLocalizer) });
        }

        var response = StockReservationMapper.ToCreateResponse(result.Reservation);

        return Created($"/stock-reservations/{result.Reservation.ReservationId}", response);
    }

    private IActionResult? ValidateRequest(FluentValidation.Results.ValidationResult validationResult)
    {
        ModelState.AddValidationErrors(validationResult);

        return ModelState.IsValid
            ? null
            : ValidationProblem(ModelState);
    }
}
