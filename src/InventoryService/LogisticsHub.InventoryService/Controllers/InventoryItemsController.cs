using LogisticsHub.InventoryService.Application.InventoryItems;
using LogisticsHub.InventoryService.Contracts;
using LogisticsHub.InventoryService.Validation;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsHub.InventoryService.Controllers;

[ApiController]
[Route("inventory-items")]
public sealed class InventoryItemsController : ControllerBase
{
    private readonly CreateInventoryItem _createInventoryItem;
    private readonly GetInventoryItem _getInventoryItem;

    public InventoryItemsController(
        CreateInventoryItem createInventoryItem,
        GetInventoryItem getInventoryItem)
    {
        _createInventoryItem = createInventoryItem;
        _getInventoryItem = getInventoryItem;
    }

    [HttpGet("{sku}")]
    public async Task<IActionResult> GetAsync(
        string sku,
        CancellationToken cancellationToken)
    {
        var result = await _getInventoryItem.ExecuteAsync(sku, cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(new GetInventoryItemResponse(
            result.Sku,
            result.Name,
            result.QuantityAvailable));
    }

    [HttpPost]
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

        var command = new CreateInventoryItemCommand(
            request.Sku.Trim(),
            request.Name.Trim(),
            request.QuantityAvailable);

        var result = await _createInventoryItem.ExecuteAsync(command, cancellationToken);

        if (result is null)
        {
            return Conflict();
        }

        var response = new CreateInventoryItemResponse(
            result.Sku,
            result.Name,
            result.QuantityAvailable);

        return Created($"/inventory-items/{result.Sku}", response);
    }
}
