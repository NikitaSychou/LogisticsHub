using FluentValidation;
using LogisticsHub.AspNetCore;
using LogisticsHub.ShipmentService.Application.Shipments;
using LogisticsHub.ShipmentService.Contracts;
using LogisticsHub.ShipmentService.Localization;
using LogisticsHub.ShipmentService.Mapping;
using LogisticsHub.ShipmentService.Validation;
using LogisticsHub.Results;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace LogisticsHub.ShipmentService.Controllers;

[ApiController]
[Route("shipments")]
public sealed class ShipmentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IStringLocalizer<ShipmentBusinessErrorMessages> _errorLocalizer;
    private readonly IValidator<CreateShipmentRequest> _validator;

    public ShipmentsController(
        IMediator mediator,
        IStringLocalizer<ShipmentBusinessErrorMessages> errorLocalizer,
        IValidator<CreateShipmentRequest> validator)
    {
        _mediator = mediator;
        _errorLocalizer = errorLocalizer;
        _validator = validator;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GetShipmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetShipmentQuery(id), cancellationToken);

        if (result.IsFailure)
        {
            return NotFound();
        }

        var response = ShipmentMapper.ToResponse(result.Value);

        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateShipmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> CreateAsync(
        CreateShipmentRequest request,
        CancellationToken cancellationToken)
    {
        var validationProblem = ValidateRequest(_validator.Validate(request));
        if (validationProblem is not null)
        {
            return validationProblem;
        }

        var command = ShipmentMapper.ToCommand(request);

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return ToCreateFailureResponse(result.Error);
        }

        var response = ShipmentMapper.ToResponse(result.Value);

        return Created($"/shipments/{response.ShipmentId}", response);
    }

    private IActionResult? ValidateRequest(FluentValidation.Results.ValidationResult validationResult)
    {
        ModelState.AddValidationErrors(validationResult);

        return ModelState.IsValid
            ? null
            : ValidationProblem(ModelState);
    }

    private IActionResult ToCreateFailureResponse(Error error)
    {
        if (error.Code == "shipment.company_service_unavailable")
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { reason = error.ToLocalizedMessage(_errorLocalizer) });
        }

        var modelStateKey = error.Code == "shipment.sender_company_address_not_found"
            ? "senderAddressId"
            : "receiverAddressId";

        ModelState.AddModelError(modelStateKey, error.ToLocalizedMessage(_errorLocalizer));
        return ValidationProblem(ModelState);
    }
}
