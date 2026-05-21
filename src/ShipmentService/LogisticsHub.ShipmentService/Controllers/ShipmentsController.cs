using LogisticsHub.ShipmentService.Application.Shipments;
using LogisticsHub.ShipmentService.Contracts;
using LogisticsHub.ShipmentService.Mapping;
using LogisticsHub.ShipmentService.Validation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsHub.ShipmentService.Controllers;

[ApiController]
[Route("shipments")]
public sealed class ShipmentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ShipmentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetShipmentQuery(id), cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        var response = ShipmentMapper.ToResponse(result);

        return Ok(response);
    }

    [HttpPost]
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

        return Created($"/shipments/{result.ShipmentId}", result);
    }
}
