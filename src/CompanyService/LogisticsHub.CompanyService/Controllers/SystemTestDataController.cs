using LogisticsHub.CompanyService.Application.Companies.GenerateCompanyTestData;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsHub.CompanyService.Controllers;

[ApiController]
public sealed class SystemTestDataController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IWebHostEnvironment _environment;

    public SystemTestDataController(
        IMediator mediator,
        IWebHostEnvironment environment)
    {
        _mediator = mediator;
        _environment = environment;
    }

    [HttpPost("/system/test-data/companies")]
    [ProducesResponseType(typeof(GenerateCompanyTestDataResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateCompaniesAsync(CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        var result = await _mediator.Send(new GenerateCompanyTestDataCommand(), cancellationToken);

        return Ok(result);
    }
}
