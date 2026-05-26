using System.Text.Json;
using LogisticsHub.IntegrationEvents.StockReservations;
using LogisticsHub.InventoryService.Application.Tests.Fakes;
using LogisticsHub.InventoryService.Domain.Entities;
using LogisticsHub.InventoryService.Outbox;
using LogisticsHub.Messaging.RabbitMQ;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LogisticsHub.InventoryService.Application.Tests.Outbox;

public sealed class InventoryOutboxProcessorTests
{
    [Fact]
    public async Task ProcessBatch_WhenPublishFails_SchedulesRetryAndKeepsMessageUnprocessed()
    {
        // Arrange
        var dbContext = new FakeInventoryDbContext();
        var outboxMessage = CreateReservedOutboxMessage();
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
        var dbContext = new FakeInventoryDbContext();
        var outboxMessage = CreateReservedOutboxMessage();
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

    [Fact]
    public async Task ProcessBatch_WhenPublishSucceeds_MarksMessageProcessed()
    {
        // Arrange
        var dbContext = new FakeInventoryDbContext();
        var outboxMessage = CreateReservedOutboxMessage();
        outboxMessage.RetryCount = 2;
        outboxMessage.Error = "previous failure";
        outboxMessage.NextAttemptAtUtc = DateTime.UtcNow.AddSeconds(-1);
        dbContext.OutboxMessages.Add(outboxMessage);
        var publisher = new FakeRabbitMqPublisher();
        var processor = CreateProcessor(dbContext, publisher);

        // Act
        var result = await processor.ProcessBatchAsync(20, "test-instance", TimeSpan.FromMinutes(5), CancellationToken.None);

        // Assert
        Assert.True(result.ProcessedAny);
        Assert.False(result.HadFailure);
        Assert.Equal(1, publisher.PublishCallCount);
        Assert.Equal(1, dbContext.SaveChangesCallCount);
        Assert.NotNull(outboxMessage.ProcessedAtUtc);
        Assert.Null(outboxMessage.NextAttemptAtUtc);
        Assert.Null(outboxMessage.Error);
        Assert.Null(outboxMessage.LockedBy);
        Assert.Null(outboxMessage.LockedAtUtc);
    }

    private static InventoryOutboxProcessor CreateProcessor(
        FakeInventoryDbContext dbContext,
        FakeRabbitMqPublisher publisher)
    {
        return new InventoryOutboxProcessor(
            dbContext,
            publisher,
            NullLogger<InventoryOutboxProcessor>.Instance);
    }

    private static InventoryOutboxMessage CreateReservedOutboxMessage()
    {
        var integrationEvent = new StockReservedIntegrationEvent(
            Guid.NewGuid(),
            DateTime.UtcNow,
            Guid.NewGuid(),
            Guid.NewGuid());

        return new InventoryOutboxMessage
        {
            Id = Guid.NewGuid(),
            OccurredAtUtc = integrationEvent.OccurredAtUtc,
            Type = typeof(StockReservedIntegrationEvent).FullName!,
            RoutingKey = StockReservationRoutingKeys.Reserved,
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
