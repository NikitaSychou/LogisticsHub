using LogisticsHub.AspNetCore;
using LogisticsHub.ShipmentService.Application.Shipments;
using LogisticsHub.ShipmentService.Contracts;
using LogisticsHub.ShipmentService.Localization;
using LogisticsHub.ShipmentService.Mapping;
using LogisticsHub.ShipmentService.Validation;
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

    public ShipmentsController(
        IMediator mediator,
        IStringLocalizer<ShipmentBusinessErrorMessages> errorLocalizer)
    {
        _mediator = mediator;
        _errorLocalizer = errorLocalizer;
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
    [ProducesResponseType(typeof(CreateShipmentResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> CreateAsync(
        CreateShipmentRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = CreateShipmentRequestValidator.Validate(request);
        ModelState.AddValidationErrors(validationErrors);

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var command = ShipmentMapper.ToCommand(request);

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error.Code == "shipment.company_service_unavailable")
            {
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new { reason = result.Error.ToLocalizedMessage(_errorLocalizer) });
            }

            var modelStateKey = result.Error.Code == "shipment.sender_company_address_not_found"
                ? "senderAddressId"
                : "receiverAddressId";

            ModelState.AddModelError(modelStateKey, result.Error.ToLocalizedMessage(_errorLocalizer));
            return ValidationProblem(ModelState);
        }

        return Created($"/shipments/{result.Value.ShipmentId}", result.Value);
    }
}
