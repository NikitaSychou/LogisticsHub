using LogisticsHub.InventoryService.Application.InventoryItems;
using LogisticsHub.InventoryService.Contracts;
using LogisticsHub.InventoryService.Controllers;
using LogisticsHub.InventoryService.Localization;
using LogisticsHub.Results;
using LogisticsHub.InventoryService.Validation;
using LogisticsHub.Results;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Xunit;

namespace LogisticsHub.InventoryService.Application.Tests.InventoryItems;

public sealed class InventoryItemsControllerTests
{
    [Fact]
    public async Task CreateAsync_WhenRequestIsInvalid_ReturnsBadRequest()
    {
        var controller = CreateController();
        var request = new CreateInventoryItemRequest("", "", -1);

        var response = await controller.CreateAsync(request, CancellationToken.None);

        AssertValidationProblem(response, StatusCodes.Status400BadRequest);
        Assert.True(controller.ModelState.ContainsKey("sku"));
        Assert.True(controller.ModelState.ContainsKey("name"));
        Assert.True(controller.ModelState.ContainsKey("quantityAvailable"));
    }

    [Fact]
    public async Task CreateAsync_WhenInventoryItemAlreadyExists_ReturnsConflict()
    {
        var controller = CreateController(new FakeMediator
        {
            CreateInventoryItemResult = Result<InventoryItemResult>.Failure(InventoryItemErrors.AlreadyExists("TEST-SKU"))
        });

        var response = await controller.CreateAsync(CreateRequest(), CancellationToken.None);

        Assert.IsType<ConflictResult>(response);
    }

    [Fact]
    public async Task GetAsync_WhenInventoryItemDoesNotExist_ReturnsNotFound()
    {
        var controller = CreateController(new FakeMediator
        {
            GetInventoryItemResult = Result<InventoryItemResult>.Failure(InventoryItemErrors.NotFound("TEST-SKU"))
        });

        var response = await controller.GetAsync("TEST-SKU", CancellationToken.None);

        Assert.IsType<NotFoundResult>(response);
    }

    [Fact]
    public async Task CreateAsync_WhenInventoryItemIsCreated_ReturnsCreated()
    {
        var result = CreateResult();
        var controller = CreateController(new FakeMediator
        {
            CreateInventoryItemResult = Result<InventoryItemResult>.Success(result)
        });

        var response = await controller.CreateAsync(CreateRequest(), CancellationToken.None);

        var createdResult = Assert.IsType<CreatedResult>(response);
        Assert.Equal($"/inventory-items/{result.Sku}", createdResult.Location);
        var value = Assert.IsAssignableFrom<CreateInventoryItemResponse>(createdResult.Value);
        Assert.Equal(result.Sku, value.Sku);
    }

    [Fact]
    public async Task ListPageAsync_WhenPageNumberIsInvalid_ReturnsBadRequest()
    {
        var controller = CreateController();

        var response = await controller.ListPageAsync(0, CancellationToken.None);

        AssertValidationProblem(response, StatusCodes.Status400BadRequest);
        Assert.True(controller.ModelState.ContainsKey("pageNumber"));
    }

    [Fact]
    public async Task ListPageAsync_WhenMoreItemsExist_ReturnsPageWithHasMore()
    {
        var controller = CreateController(new FakeMediator
        {
            ListInventoryItemsPageResult = new PagedResponse<InventoryItemResult>(
                [CreateResult("SKU-001"), CreateResult("SKU-002")],
                1,
                2,
                true)
        }, pageSize: 2);

        var response = await controller.ListPageAsync(1, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(response);
        var value = Assert.IsAssignableFrom<PagedResponse<GetInventoryItemResponse>>(okResult.Value);
        Assert.Equal(1, value.PageNumber);
        Assert.Equal(2, value.PageSize);
        Assert.True(value.HasMore);
        Assert.Equal(["SKU-001", "SKU-002"], value.Items.Select(item => item.Sku));
    }

    [Fact]
    public async Task ListPageAsync_WhenFinalPageIsReturned_ReturnsHasMoreFalse()
    {
        var controller = CreateController(new FakeMediator
        {
            ListInventoryItemsPageResult = new PagedResponse<InventoryItemResult>(
                [CreateResult("SKU-003")],
                2,
                2,
                false)
        }, pageSize: 2);

        var response = await controller.ListPageAsync(2, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(response);
        var value = Assert.IsAssignableFrom<PagedResponse<GetInventoryItemResponse>>(okResult.Value);
        Assert.Equal(2, value.PageNumber);
        Assert.Equal(2, value.PageSize);
        Assert.False(value.HasMore);
        Assert.Single(value.Items);
    }

    private static InventoryItemsController CreateController()
    {
        return CreateController(new FakeMediator());
    }

    private static InventoryItemsController CreateController(FakeMediator mediator, int pageSize = 50)
    {
        var serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddControllers()
            .Services
            .BuildServiceProvider();

        return new InventoryItemsController(
            mediator,
            new CreateInventoryItemRequestValidator(new FakeInventoryValidationLocalizer()),
            Options.Create(new PaginationOptions
            {
                DefaultPageSize = pageSize,
                MaxPageSize = pageSize
            }))
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

    private static CreateInventoryItemRequest CreateRequest()
    {
        return new CreateInventoryItemRequest("TEST-SKU", "Test item", 5);
    }

    private static InventoryItemResult CreateResult()
    {
        return new InventoryItemResult("TEST-SKU", "Test item", 5);
    }

    private static InventoryItemResult CreateResult(string sku)
    {
        return new InventoryItemResult(sku, $"Test item {sku}", 5);
    }

    private static void AssertValidationProblem(IActionResult response, int expectedStatusCode)
    {
        var objectResult = Assert.IsAssignableFrom<ObjectResult>(response);
        var problemDetails = Assert.IsAssignableFrom<ProblemDetails>(objectResult.Value);

        Assert.Equal(expectedStatusCode, objectResult.StatusCode ?? problemDetails.Status);
    }

    private sealed class FakeMediator : IMediator
    {
        public Result<InventoryItemResult> CreateInventoryItemResult { get; set; } =
            Result<InventoryItemResult>.Success(InventoryItemsControllerTests.CreateResult());

        public Result<InventoryItemResult> GetInventoryItemResult { get; set; } =
            Result<InventoryItemResult>.Success(InventoryItemsControllerTests.CreateResult());

        public PagedResponse<InventoryItemResult> ListInventoryItemsPageResult { get; set; } =
            new([InventoryItemsControllerTests.CreateResult()], 1, 50, false);

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            object result = request switch
            {
                CreateInventoryItemCommand => CreateInventoryItemResult,
                GetInventoryItemQuery => GetInventoryItemResult,
                ListInventoryItemsPageQuery => ListInventoryItemsPageResult,
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

    private sealed class FakeInventoryValidationLocalizer : IStringLocalizer<InventoryValidationMessages>
    {
        public LocalizedString this[string name] => new(name, name);

        public LocalizedString this[string name, params object[] arguments] => new(name, string.Format(name, arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            return [];
        }
    }
}
