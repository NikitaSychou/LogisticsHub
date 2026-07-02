using LogisticsHub.AspNetCore;
using LogisticsHub.CompanyService.Application.Companies;
using LogisticsHub.CompanyService.Application.Companies.GenerateCompanyTestData;
using LogisticsHub.CompanyService.Application.Companies.GetCompanyAddress;
using LogisticsHub.CompanyService.Contracts;
using LogisticsHub.CompanyService.Localization;
using LogisticsHub.CompanyService.Mapping;
using LogisticsHub.CompanyService.Validation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace LogisticsHub.CompanyService.Controllers;

[ApiController]
[Route("companies")]
public sealed class CompaniesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IStringLocalizer<CompanyBusinessErrorMessages> _errorLocalizer;

    public CompaniesController(
        IMediator mediator,
        IStringLocalizer<CompanyBusinessErrorMessages> errorLocalizer)
    {
        _mediator = mediator;
        _errorLocalizer = errorLocalizer;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CompanyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAsync(
        CreateCompanyRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = CompanyRequestValidator.Validate(request);
        ModelState.AddValidationErrors(validationErrors);

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await _mediator.Send(CompanyMapper.ToCommand(request), cancellationToken);

        if (result.IsFailure)
        {
            return Conflict(new { reason = result.Error.ToLocalizedMessage(_errorLocalizer) });
        }

        var response = CompanyMapper.ToResponse(result.Value);

        return Created($"/companies/{response.Id}", response);
    }

    [HttpPost("/system/test-data/companies")]
    [ProducesResponseType(typeof(GenerateCompanyTestDataResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateTestDataAsync(
        [FromServices] IWebHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        var result = await _mediator.Send(new GenerateCompanyTestDataCommand(), cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CompanyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCompanyQuery(id), cancellationToken);

        if (result.IsFailure)
        {
            return NotFound();
        }

        return Ok(CompanyMapper.ToResponse(result.Value));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CompanyResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ListCompaniesQuery(), cancellationToken);
        var response = result
            .Select(CompanyMapper.ToResponse)
            .ToArray();

        return Ok(response);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CompanyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateAsync(
        Guid id,
        UpdateCompanyRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = CompanyRequestValidator.Validate(request);
        ModelState.AddValidationErrors(validationErrors);

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await _mediator.Send(CompanyMapper.ToCommand(id, request), cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error.Code == "company.not_found")
            {
                return NotFound();
            }

            return Conflict(new { reason = result.Error.ToLocalizedMessage(_errorLocalizer) });
        }

        return Ok(CompanyMapper.ToResponse(result.Value));
    }

    [HttpPost("{companyId:guid}/addresses")]
    [ProducesResponseType(typeof(CompanyAddressResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateAddressAsync(
        Guid companyId,
        CreateCompanyAddressRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = CompanyAddressRequestValidator.Validate(request);
        ModelState.AddValidationErrors(validationErrors);

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await _mediator.Send(CompanyMapper.ToCommand(companyId, request), cancellationToken);

        if (result.IsFailure)
        {
            return NotFound();
        }

        var response = CompanyMapper.ToResponse(result.Value);

        return Created($"/companies/{companyId}/addresses/{response.Id}", response);
    }

    [HttpGet("{companyId:guid}/addresses")]
    [ProducesResponseType(typeof(IReadOnlyList<CompanyAddressResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListAddressesAsync(
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ListCompanyAddressesQuery(companyId), cancellationToken);

        if (result.IsFailure)
        {
            return NotFound();
        }

        var response = result.Value
            .Select(CompanyMapper.ToResponse)
            .ToArray();

        return Ok(response);
    }

    [HttpGet("{companyId:guid}/addresses/{addressId:guid}")]
    [ProducesResponseType(typeof(CompanyAddressResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAddressAsync(
        Guid companyId,
        Guid addressId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCompanyAddressQuery(companyId, addressId), cancellationToken);

        if (result.IsFailure)
        {
            return NotFound();
        }

        return Ok(CompanyMapper.ToResponse(result.Value));
    }
}
