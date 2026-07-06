using LogisticsHub.CompanyService.Application.Companies.Addresses.ListCompanyAddresses;
using LogisticsHub.CompanyService.Application.Companies.Addresses.CreateCompanyAddress;
using LogisticsHub.CompanyService.Application.Companies.Company.UpdateCompany;
using LogisticsHub.CompanyService.Application.Companies.Company.ListCompanies;
using LogisticsHub.CompanyService.Application.Companies.Company.GetCompany;
using LogisticsHub.CompanyService.Application.Companies.Company.CreateCompany;
using LogisticsHub.CompanyService.Application.Companies;
using LogisticsHub.CompanyService.Application.Companies.Results;
using LogisticsHub.CompanyService.Application.Companies.Addresses.GetCompanyAddress;
using LogisticsHub.CompanyService.Contracts;
using LogisticsHub.CompanyService.Controllers;
using LogisticsHub.CompanyService.Domain.Enums;
using LogisticsHub.CompanyService.Localization;
using LogisticsHub.CompanyService.Validation;
using LogisticsHub.Results;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Xunit;

namespace LogisticsHub.CompanyService.Tests.Companies;

public sealed class CompaniesControllerTests
{
    [Fact]
    public async Task CreateAsync_WhenRequestIsInvalid_ReturnsBadRequest()
    {
        var controller = CreateController();
        var request = new CreateCompanyRequest(Name: null, ExternalCode: null, Status: null);

        var response = await controller.CreateAsync(request, CancellationToken.None);

        AssertValidationProblem(response, StatusCodes.Status400BadRequest);
        Assert.True(controller.ModelState.ContainsKey("name"));
        Assert.True(controller.ModelState.ContainsKey("status"));
    }

    [Fact]
    public async Task CreateAsync_WhenExternalCodeAlreadyExists_ReturnsConflict()
    {
        var controller = CreateController(new FakeMediator
        {
            CreateCompanyResult = Result<CompanyResult>.Failure(CompanyErrors.ExternalCodeAlreadyExists("ACME"))
        });

        var response = await controller.CreateAsync(CreateCompanyRequest(), CancellationToken.None);

        var objectResult = Assert.IsAssignableFrom<ObjectResult>(response);
        Assert.Equal(StatusCodes.Status409Conflict, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetAsync_WhenCompanyDoesNotExist_ReturnsNotFound()
    {
        var companyId = Guid.NewGuid();
        var controller = CreateController(new FakeMediator
        {
            GetCompanyResult = Result<CompanyResult>.Failure(CompanyErrors.NotFound(companyId))
        });

        var response = await controller.GetAsync(companyId, CancellationToken.None);

        Assert.IsType<NotFoundResult>(response);
    }

    [Fact]
    public async Task UpdateAsync_WhenCompanyDoesNotExist_ReturnsNotFound()
    {
        var companyId = Guid.NewGuid();
        var controller = CreateController(new FakeMediator
        {
            UpdateCompanyResult = Result<CompanyResult>.Failure(CompanyErrors.NotFound(companyId))
        });

        var response = await controller.UpdateAsync(companyId, UpdateCompanyRequest(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(response);
    }

    [Fact]
    public async Task UpdateAsync_WhenExternalCodeAlreadyExists_ReturnsConflict()
    {
        var controller = CreateController(new FakeMediator
        {
            UpdateCompanyResult = Result<CompanyResult>.Failure(CompanyErrors.ExternalCodeAlreadyExists("ACME"))
        });

        var response = await controller.UpdateAsync(Guid.NewGuid(), UpdateCompanyRequest(), CancellationToken.None);

        var objectResult = Assert.IsAssignableFrom<ObjectResult>(response);
        Assert.Equal(StatusCodes.Status409Conflict, objectResult.StatusCode);
    }

    [Fact]
    public async Task CreateAddressAsync_WhenRequestIsInvalid_ReturnsBadRequest()
    {
        var controller = CreateController();
        var request = new CreateCompanyAddressRequest(
            AddressType: null,
            CountryCode: null,
            City: null,
            PostalCode: null,
            Line1: null,
            Line2: null);

        var response = await controller.CreateAddressAsync(Guid.NewGuid(), request, CancellationToken.None);

        AssertValidationProblem(response, StatusCodes.Status400BadRequest);
        Assert.True(controller.ModelState.ContainsKey("addressType"));
        Assert.True(controller.ModelState.ContainsKey("countryCode"));
        Assert.True(controller.ModelState.ContainsKey("city"));
        Assert.True(controller.ModelState.ContainsKey("line1"));
    }

    [Fact]
    public async Task CreateAddressAsync_WhenCompanyDoesNotExist_ReturnsNotFound()
    {
        var companyId = Guid.NewGuid();
        var controller = CreateController(new FakeMediator
        {
            CreateCompanyAddressResult = Result<CompanyAddressResult>.Failure(CompanyErrors.AddressCompanyNotFound(companyId))
        });

        var response = await controller.CreateAddressAsync(
            companyId,
            CreateCompanyAddressRequest(),
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(response);
    }

    [Fact]
    public async Task GetAddressAsync_WhenAddressExists_ReturnsOk()
    {
        var addressResult = CreateAddressResult();
        var controller = CreateController(new FakeMediator
        {
            GetCompanyAddressResult = Result<CompanyAddressResult>.Success(addressResult)
        });

        var response = await controller.GetAddressAsync(addressResult.CompanyId, addressResult.Id, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(response);
        var value = Assert.IsAssignableFrom<CompanyAddressResponse>(okResult.Value);
        Assert.Equal(addressResult.Id, value.Id);
    }

    [Fact]
    public async Task GetAddressAsync_WhenAddressDoesNotExist_ReturnsNotFound()
    {
        var companyId = Guid.NewGuid();
        var controller = CreateController(new FakeMediator
        {
            GetCompanyAddressResult = Result<CompanyAddressResult>.Failure(CompanyErrors.AddressCompanyNotFound(companyId))
        });

        var response = await controller.GetAddressAsync(companyId, Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(response);
    }

    private static CompaniesController CreateController()
    {
        return CreateController(new FakeMediator());
    }

    private static CompaniesController CreateController(FakeMediator mediator)
    {
        var serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddControllers()
            .Services
            .BuildServiceProvider();

        return new CompaniesController(
            mediator,
            new FakeCompanyBusinessErrorLocalizer(),
            new CreateCompanyRequestValidator(new FakeCompanyValidationLocalizer()),
            new UpdateCompanyRequestValidator(new FakeCompanyValidationLocalizer()),
            new CompanyAddressRequestValidator(new FakeCompanyValidationLocalizer()))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    RequestServices = serviceProvider
                }
            }
        };
    }

    private static CreateCompanyRequest CreateCompanyRequest()
    {
        return new CreateCompanyRequest("Acme Logistics", "ACME", "Active");
    }

    private static UpdateCompanyRequest UpdateCompanyRequest()
    {
        return new UpdateCompanyRequest("Acme Logistics", "ACME", "Active");
    }

    private static CreateCompanyAddressRequest CreateCompanyAddressRequest()
    {
        return new CreateCompanyAddressRequest(
            "Warehouse",
            "US",
            "Chicago",
            "60601",
            "100 Main Street",
            null);
    }

    private static CompanyResult CreateCompanyResult()
    {
        return new CompanyResult(
            Guid.NewGuid(),
            "Acme Logistics",
            "ACME",
            CompanyStatus.Active,
            DateTime.UtcNow,
            UpdatedAtUtc: null);
    }

    private static CompanyAddressResult CreateAddressResult()
    {
        return new CompanyAddressResult(
            Guid.NewGuid(),
            Guid.NewGuid(),
            CompanyAddressType.Warehouse,
            "US",
            "Chicago",
            "60601",
            "100 Main Street",
            Line2: null,
            DateTime.UtcNow,
            UpdatedAtUtc: null);
    }

    private static void AssertValidationProblem(IActionResult response, int expectedStatusCode)
    {
        var objectResult = Assert.IsAssignableFrom<ObjectResult>(response);
        var problemDetails = Assert.IsAssignableFrom<ProblemDetails>(objectResult.Value);

        Assert.Equal(expectedStatusCode, objectResult.StatusCode ?? problemDetails.Status);
    }

    private sealed class FakeMediator : IMediator
    {
        public Result<CompanyResult> CreateCompanyResult { get; set; } =
            Result<CompanyResult>.Success(CompaniesControllerTests.CreateCompanyResult());

        public Result<CompanyResult> GetCompanyResult { get; set; } =
            Result<CompanyResult>.Success(CompaniesControllerTests.CreateCompanyResult());

        public Result<CompanyResult> UpdateCompanyResult { get; set; } =
            Result<CompanyResult>.Success(CompaniesControllerTests.CreateCompanyResult());

        public Result<CompanyAddressResult> CreateCompanyAddressResult { get; set; } =
            Result<CompanyAddressResult>.Success(CompaniesControllerTests.CreateAddressResult());

        public Result<CompanyAddressResult> GetCompanyAddressResult { get; set; } =
            Result<CompanyAddressResult>.Success(CompaniesControllerTests.CreateAddressResult());

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            object result = request switch
            {
                CreateCompanyCommand => CreateCompanyResult,
                GetCompanyQuery => GetCompanyResult,
                UpdateCompanyCommand => UpdateCompanyResult,
                CreateCompanyAddressCommand => CreateCompanyAddressResult,
                GetCompanyAddressQuery => GetCompanyAddressResult,
                ListCompaniesQuery => Array.Empty<CompanyResult>(),
                ListCompanyAddressesQuery => Array.Empty<CompanyAddressResult>(),
                _ => throw new InvalidOperationException($"Unexpected request type '{request.GetType().Name}'.")
            };

            return Task.FromResult((TResponse)result);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
        {
            throw new InvalidOperationException($"Unexpected request type '{typeof(TRequest).Name}'.");
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException($"Unexpected request type '{request.GetType().Name}'.");
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException($"Unexpected stream request type '{request.GetType().Name}'.");
        }

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException($"Unexpected stream request type '{request.GetType().Name}'.");
        }

        public Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException($"Unexpected notification type '{notification.GetType().Name}'.");
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            throw new InvalidOperationException($"Unexpected notification type '{typeof(TNotification).Name}'.");
        }
    }

    private sealed class FakeCompanyBusinessErrorLocalizer : IStringLocalizer<CompanyBusinessErrorMessages>
    {
        public LocalizedString this[string name] => new(name, name);

        public LocalizedString this[string name, params object[] arguments] => new(name, string.Format(name, arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            return [];
        }
    }

    private sealed class FakeCompanyValidationLocalizer : IStringLocalizer<CompanyValidationMessages>
    {
        public LocalizedString this[string name] => new(name, name);

        public LocalizedString this[string name, params object[] arguments] => new(name, string.Format(name, arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            return [];
        }
    }
}
