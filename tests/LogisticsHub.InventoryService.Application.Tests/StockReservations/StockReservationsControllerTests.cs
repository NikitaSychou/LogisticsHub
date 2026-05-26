using LogisticsHub.InventoryService.Application.StockReservations;
using LogisticsHub.InventoryService.Contracts;
using LogisticsHub.InventoryService.Controllers;
using LogisticsHub.InventoryService.Domain.Enums;
using LogisticsHub.InventoryService.Localization;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Xunit;

namespace LogisticsHub.InventoryService.Application.Tests.StockReservations;

public sealed class StockReservationsControllerTests
{
    [Fact]
    public async Task CreateAsync_WhenRequestIsInvalid_ReturnsBadRequest()
    {
        var controller = CreateController();
        var request = new CreateStockReservationRequest(Guid.Empty, Items: []);

        var response = await controller.CreateAsync(request, CancellationToken.None);

        AssertValidationProblem(response, StatusCodes.Status400BadRequest);
        Assert.True(controller.ModelState.ContainsKey("shipmentId"));
        Assert.True(controller.ModelState.ContainsKey("items"));
    }

    [Fact]
    public async Task CreateAsync_WhenStockReservationCannotBeCreated_ReturnsConflict()
    {
        var controller = CreateController(new FakeMediator
        {
            CreateStockReservationResult = new CreateStockReservationResult(
                Reservation: null,
                StockReservationErrors.InsufficientStock("TEST-SKU"))
        });

        var response = await controller.CreateAsync(CreateRequest(), CancellationToken.None);

        var objectResult = Assert.IsAssignableFrom<ObjectResult>(response);
        Assert.Equal(StatusCodes.Status409Conflict, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetAsync_WhenStockReservationDoesNotExist_ReturnsNotFound()
    {
        var reservationId = Guid.NewGuid();
        var controller = CreateController(new FakeMediator
        {
            GetStockReservationResult = LogisticsHub.Results.Result<StockReservationResult>.Failure(
                StockReservationErrors.NotFound(reservationId))
        });

        var response = await controller.GetAsync(reservationId, CancellationToken.None);

        Assert.IsType<NotFoundResult>(response);
    }

    [Fact]
    public async Task CreateAsync_WhenStockReservationIsCreated_ReturnsCreated()
    {
        var result = CreateResult();
        var controller = CreateController(new FakeMediator
        {
            CreateStockReservationResult = new CreateStockReservationResult(
                result,
                StockReservationErrors.ConcurrencyFailure)
        });

        var response = await controller.CreateAsync(CreateRequest(), CancellationToken.None);

        var createdResult = Assert.IsType<CreatedResult>(response);
        Assert.Equal($"/stock-reservations/{result.ReservationId}", createdResult.Location);
        var value = Assert.IsAssignableFrom<CreateStockReservationResponse>(createdResult.Value);
        Assert.Equal(result.ReservationId, value.ReservationId);
    }

    private static StockReservationsController CreateController()
    {
        return CreateController(new FakeMediator());
    }

    private static StockReservationsController CreateController(FakeMediator mediator)
    {
        var serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddControllers()
            .Services
            .BuildServiceProvider();

        return new StockReservationsController(mediator, new FakeInventoryBusinessErrorLocalizer())
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

    private static CreateStockReservationRequest CreateRequest()
    {
        return new CreateStockReservationRequest(
            Guid.NewGuid(),
            [new CreateStockReservationItemRequest("TEST-SKU", 2)]);
    }

    private static StockReservationResult CreateResult()
    {
        return new StockReservationResult(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ReservationStatus.Active,
            [new StockReservationItemResult("TEST-SKU", 2)]);
    }

    private static void AssertValidationProblem(IActionResult response, int expectedStatusCode)
    {
        var objectResult = Assert.IsAssignableFrom<ObjectResult>(response);
        var problemDetails = Assert.IsAssignableFrom<ProblemDetails>(objectResult.Value);

        Assert.Equal(expectedStatusCode, objectResult.StatusCode ?? problemDetails.Status);
    }

    private sealed class FakeMediator : IMediator
    {
        public CreateStockReservationResult CreateStockReservationResult { get; set; } =
            new(StockReservationsControllerTests.CreateResult(), StockReservationErrors.ConcurrencyFailure);

        public LogisticsHub.Results.Result<StockReservationResult> GetStockReservationResult { get; set; } =
            LogisticsHub.Results.Result<StockReservationResult>.Success(StockReservationsControllerTests.CreateResult());

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            object result = request switch
            {
                CreateStockReservationCommand => CreateStockReservationResult,
                GetStockReservationQuery => GetStockReservationResult,
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

    private sealed class FakeInventoryBusinessErrorLocalizer : IStringLocalizer<InventoryBusinessErrorMessages>
    {
        public LocalizedString this[string name] => new(name, name);

        public LocalizedString this[string name, params object[] arguments] => new(name, string.Format(name, arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            return [];
        }
    }
}
