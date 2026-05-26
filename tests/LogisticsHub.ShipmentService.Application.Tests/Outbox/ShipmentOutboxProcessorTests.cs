using System.Text.Json;
using LogisticsHub.IntegrationEvents.StockReservations;
using LogisticsHub.Messaging.RabbitMQ;
using LogisticsHub.ShipmentService.Application.Tests.Fakes;
using LogisticsHub.ShipmentService.Domain.Entities;
using LogisticsHub.ShipmentService.Outbox;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LogisticsHub.ShipmentService.Application.Tests.Outbox;

public sealed class ShipmentOutboxProcessorTests
{
    [Fact]
    public async Task ProcessBatch_WhenPublishFails_SchedulesRetryAndKeepsMessageUnprocessed()
    {
        // Arrange
        var dbContext = new FakeShipmentDbContext();
        var outboxMessage = CreateRequestedOutboxMessage();
        dbContext.OutboxMessages.Add(outboxMessage);
        var publisher = new FakeRabbitMqPublisher
        {
            ExceptionToThrow = new InvalidOperationException("broker unavailable")
        };
        var processor = CreateProcessor(dbContext, publisher);

        // Act
        var result = await processor.ProcessBatchAsync(20, "test-instance", TimeSpan.FromMinutes(5), CancellationToken.None);

        // Assert
        Assert.True(result.ProcessedAny);
        Assert.True(result.HadFailure);
        Assert.Equal(1, publisher.PublishCallCount);
        Assert.Equal(1, dbContext.SaveChangesCallCount);
        Assert.Null(outboxMessage.ProcessedAtUtc);
        Assert.Null(outboxMessage.FailedAtUtc);
        Assert.Null(outboxMessage.LockedBy);
        Assert.Null(outboxMessage.LockedAtUtc);
        Assert.Equal(1, outboxMessage.RetryCount);
        Assert.NotNull(outboxMessage.NextAttemptAtUtc);
        Assert.Contains("broker unavailable", outboxMessage.Error);
    }

    [Fact]
    public async Task ProcessBatch_WhenPublishFailsAtMaxRetry_MarksMessageFailed()
    {
        // Arrange
        var dbContext = new FakeShipmentDbContext();
        var outboxMessage = CreateRequestedOutboxMessage();
        outboxMessage.RetryCount = 4;
        dbContext.OutboxMessages.Add(outboxMessage);
        var publisher = new FakeRabbitMqPublisher
        {
            ExceptionToThrow = new InvalidOperationException("confirm failed")
        };
        var processor = CreateProcessor(dbContext, publisher);

        // Act
        var result = await processor.ProcessBatchAsync(20, "test-instance", TimeSpan.FromMinutes(5), CancellationToken.None);

        // Assert
        Assert.True(result.ProcessedAny);
        Assert.True(result.HadFailure);
        Assert.Equal(1, publisher.PublishCallCount);
        Assert.Equal(1, dbContext.SaveChangesCallCount);
        Assert.Null(outboxMessage.ProcessedAtUtc);
        Assert.NotNull(outboxMessage.FailedAtUtc);
        Assert.Null(outboxMessage.NextAttemptAtUtc);
        Assert.Null(outboxMessage.LockedBy);
        Assert.Null(outboxMessage.LockedAtUtc);
        Assert.Equal(5, outboxMessage.RetryCount);
        Assert.Contains("confirm failed", outboxMessage.Error);
    }

    private static ShipmentOutboxProcessor CreateProcessor(
        FakeShipmentDbContext dbContext,
        FakeRabbitMqPublisher publisher)
    {
        return new ShipmentOutboxProcessor(
            dbContext,
            publisher,
            NullLogger<ShipmentOutboxProcessor>.Instance);
    }

    private static ShipmentOutboxMessage CreateRequestedOutboxMessage()
    {
        var integrationEvent = new StockReservationRequestedIntegrationEvent(
            Guid.NewGuid(),
            DateTime.UtcNow,
            Guid.NewGuid(),
            [new StockReservationRequestedItem("TEST-SKU-001", 5)]);

        return new ShipmentOutboxMessage
        {
            Id = Guid.NewGuid(),
            OccurredAtUtc = integrationEvent.OccurredAtUtc,
            Type = typeof(StockReservationRequestedIntegrationEvent).FullName!,
            RoutingKey = StockReservationRoutingKeys.Requested,
            Payload = JsonSerializer.Serialize(integrationEvent, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    private sealed class FakeRabbitMqPublisher : IRabbitMqPublisher
    {
        public int PublishCallCount { get; private set; }
        public Exception? ExceptionToThrow { get; init; }

        public Task PublishAsync<TMessage>(
            string routingKey,
            TMessage message,
            CancellationToken cancellationToken = default)
        {
            PublishCallCount++;

            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            return Task.CompletedTask;
        }
    }
}
