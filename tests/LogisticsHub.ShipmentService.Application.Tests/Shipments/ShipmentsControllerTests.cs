using LogisticsHub.Results;
using LogisticsHub.ShipmentService.Application.Shipments;
using LogisticsHub.ShipmentService.Contracts;
using LogisticsHub.ShipmentService.Controllers;
using LogisticsHub.ShipmentService.Domain.Enums;
using LogisticsHub.ShipmentService.Localization;
using LogisticsHub.ShipmentService.Validation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Xunit;

namespace LogisticsHub.ShipmentService.Application.Tests.Shipments;

public sealed class ShipmentsControllerTests
{
    [Fact]
    public async Task GetAsync_WhenShipmentExists_ReturnsReferencesWithoutLegacyDestinationFields()
    {
        var result = CreateGetResult();
        var controller = CreateController(new FakeMediator(getShipmentResult: Result<GetShipmentResult>.Success(result)));

        var response = await controller.GetAsync(result.ShipmentId, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(response);
        var shipment = Assert.IsType<GetShipmentResponse>(okResult.Value);
        Assert.Equal(result.SenderCompanyId, shipment.SenderCompanyId);
        Assert.Equal(result.SenderAddressId, shipment.SenderAddressId);
        Assert.Equal(result.ReceiverCompanyId, shipment.ReceiverCompanyId);
        Assert.Equal(result.ReceiverAddressId, shipment.ReceiverAddressId);
        Assert.DoesNotContain(
            typeof(GetShipmentResponse).GetProperties(),
            property => property.Name is "DestinationName" or "DestinationAddress");
    }

    [Fact]
    public async Task CreateAsync_WhenCompanyAddressReferencesAreMissing_ReturnsBadRequest()
    {
        var controller = CreateController(Result<CreateShipmentResult>.Success(CreateResult()));
        var request = new CreateShipmentRequest(
            [new CreateShipmentItemRequest("TEST-SKU-001", 1)],
            SenderCompanyId: null,
            SenderAddressId: null,
            ReceiverCompanyId: null,
            ReceiverAddressId: null);

        var response = await controller.CreateAsync(request, CancellationToken.None);

        AssertValidationProblem(response, StatusCodes.Status400BadRequest);
        Assert.True(controller.ModelState.ContainsKey("companyAddressReferences"));
    }

    [Fact]
    public async Task CreateAsync_WhenSenderReferenceDoesNotExist_ReturnsBadRequest()
    {
        var senderCompanyId = Guid.NewGuid();
        var senderAddressId = Guid.NewGuid();
        var controller = CreateController(Result<CreateShipmentResult>.Failure(
            ShipmentErrors.SenderCompanyAddressNotFound(senderCompanyId, senderAddressId)));

        var response = await controller.CreateAsync(CreateRequest(senderCompanyId, senderAddressId), CancellationToken.None);

        AssertValidationProblem(response, StatusCodes.Status400BadRequest);
        Assert.True(controller.ModelState.ContainsKey("senderAddressId"));
    }

    [Fact]
    public async Task CreateAsync_WhenReceiverReferenceDoesNotExist_ReturnsBadRequest()
    {
        var receiverCompanyId = Guid.NewGuid();
        var receiverAddressId = Guid.NewGuid();
        var controller = CreateController(Result<CreateShipmentResult>.Failure(
            ShipmentErrors.ReceiverCompanyAddressNotFound(receiverCompanyId, receiverAddressId)));

        var response = await controller.CreateAsync(
            CreateRequest(receiverCompanyId: receiverCompanyId, receiverAddressId: receiverAddressId),
            CancellationToken.None);

        AssertValidationProblem(response, StatusCodes.Status400BadRequest);
        Assert.True(controller.ModelState.ContainsKey("receiverAddressId"));
    }

    [Fact]
    public async Task CreateAsync_WhenCompanyServiceIsUnavailable_ReturnsServiceUnavailable()
    {
        var controller = CreateController(Result<CreateShipmentResult>.Failure(ShipmentErrors.CompanyServiceUnavailable()));

        var response = await controller.CreateAsync(CreateRequest(), CancellationToken.None);

        var objectResult = Assert.IsAssignableFrom<ObjectResult>(response);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, objectResult.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_WhenCompanyServiceValidationTimesOut_ReturnsServiceUnavailable()
    {
        var controller = CreateController(Result<CreateShipmentResult>.Failure(ShipmentErrors.CompanyServiceUnavailable()));

        var response = await controller.CreateAsync(CreateRequest(), CancellationToken.None);

        var objectResult = Assert.IsAssignableFrom<ObjectResult>(response);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, objectResult.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_WhenShipmentIsCreated_ReturnsCreated()
    {
        var result = CreateResult();
        var mediator = new FakeMediator(Result<CreateShipmentResult>.Success(result));
        var controller = CreateController(mediator);
        var request = CreateRequest();

        var response = await controller.CreateAsync(request, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedResult>(response);
        Assert.Equal($"/shipments/{result.ShipmentId}", createdResult.Location);
        var value = Assert.IsType<CreateShipmentResponse>(createdResult.Value);
        Assert.Equal(result.ShipmentId, value.ShipmentId);
        Assert.Equal(result.Status, value.Status);
        Assert.Equal(result.SenderCompanyId, value.SenderCompanyId);
        Assert.Equal(result.SenderAddressId, value.SenderAddressId);
        Assert.Equal(result.ReceiverCompanyId, value.ReceiverCompanyId);
        Assert.Equal(result.ReceiverAddressId, value.ReceiverAddressId);
        Assert.NotNull(mediator.CreateShipmentCommand);
        Assert.Equal(request.SenderCompanyId, mediator.CreateShipmentCommand.SenderCompanyId);
        Assert.Equal(request.SenderAddressId, mediator.CreateShipmentCommand.SenderAddressId);
        Assert.Equal(request.ReceiverCompanyId, mediator.CreateShipmentCommand.ReceiverCompanyId);
        Assert.Equal(request.ReceiverAddressId, mediator.CreateShipmentCommand.ReceiverAddressId);
    }

    private static ShipmentsController CreateController(Result<CreateShipmentResult> createShipmentResult)
    {
        return CreateController(new FakeMediator(createShipmentResult));
    }

    private static ShipmentsController CreateController(FakeMediator mediator)
    {
        var serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddControllers()
            .Services
            .BuildServiceProvider();

        return new ShipmentsController(
            mediator,
            new FakeShipmentBusinessErrorLocalizer(),
            new CreateShipmentRequestValidator())
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

    private static CreateShipmentRequest CreateRequest(
        Guid? senderCompanyId = null,
        Guid? senderAddressId = null,
        Guid? receiverCompanyId = null,
        Guid? receiverAddressId = null)
    {
        return new CreateShipmentRequest(
            [new CreateShipmentItemRequest("TEST-SKU-001", 1)],
            senderCompanyId ?? Guid.NewGuid(),
            senderAddressId ?? Guid.NewGuid(),
            receiverCompanyId ?? Guid.NewGuid(),
            receiverAddressId ?? Guid.NewGuid());
    }

    private static CreateShipmentResult CreateResult()
    {
        return new CreateShipmentResult(
            Guid.NewGuid(),
            ShipmentStatus.ReservationRequested,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid());
    }

    private static GetShipmentResult CreateGetResult()
    {
        return new GetShipmentResult(
            Guid.NewGuid(),
            "SHP-TEST",
            ShipmentStatus.ReservationRequested,
            null,
            null,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            DateTime.UtcNow,
            DateTime.UtcNow,
            null,
            null,
            [new GetShipmentItemResult("TEST-SKU-001", 1)]);
    }

    private static void AssertValidationProblem(IActionResult response, int expectedStatusCode)
    {
        var objectResult = Assert.IsAssignableFrom<ObjectResult>(response);
        var problemDetails = Assert.IsAssignableFrom<ProblemDetails>(objectResult.Value);

        Assert.Equal(expectedStatusCode, objectResult.StatusCode ?? problemDetails.Status);
    }

    private sealed class FakeMediator : IMediator
    {
        private readonly Result<CreateShipmentResult>? _createShipmentResult;
        private readonly Result<GetShipmentResult>? _getShipmentResult;

        public FakeMediator(
            Result<CreateShipmentResult>? createShipmentResult = null,
            Result<GetShipmentResult>? getShipmentResult = null)
        {
            _createShipmentResult = createShipmentResult;
            _getShipmentResult = getShipmentResult;
        }

        public CreateShipmentCommand? CreateShipmentCommand { get; private set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is CreateShipmentCommand command &&
                _createShipmentResult is TResponse createShipmentResult)
            {
                CreateShipmentCommand = command;
                return Task.FromResult(createShipmentResult);
            }

            if (request is GetShipmentQuery &&
                _getShipmentResult is TResponse getShipmentResult)
            {
                return Task.FromResult(getShipmentResult);
            }

            throw new InvalidOperationException($"Unexpected request type '{request.GetType().Name}'.");
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

    private sealed class FakeShipmentBusinessErrorLocalizer : IStringLocalizer<ShipmentBusinessErrorMessages>
    {
        public LocalizedString this[string name] => new(name, name);

        public LocalizedString this[string name, params object[] arguments] => new(name, string.Format(name, arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            return [];
        }
    }
}
