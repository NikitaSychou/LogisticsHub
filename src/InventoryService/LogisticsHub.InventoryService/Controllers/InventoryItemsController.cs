using LogisticsHub.InventoryService.Application.InventoryItems;
using LogisticsHub.InventoryService.Contracts;
using LogisticsHub.InventoryService.Mapping;
using LogisticsHub.InventoryService.Validation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsHub.InventoryService.Controllers;

[ApiController]
[Route("inventory-items")]
public sealed class InventoryItemsController : ControllerBase
{
    private readonly IMediator _mediator;

    public InventoryItemsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{sku}")]
    [ProducesResponseType(typeof(GetInventoryItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsync(
        string sku,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetInventoryItemQuery(sku), cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(InventoryItemMapper.ToGetResponse(result));
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateInventoryItemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAsync(
        CreateInventoryItemRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = CreateInventoryItemRequestValidator.Validate(request);
        ModelState.AddValidationErrors(validationErrors);

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var command = InventoryItemMapper.ToCommand(request);

        var result = await _mediator.Send(command, cancellationToken);

        if (result is null)
        {
            return Conflict();
        }

        var response = InventoryItemMapper.ToCreateResponse(result);

        return Created($"/inventory-items/{result.Sku}", response);
    }
}
