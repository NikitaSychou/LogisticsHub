using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using LogisticsHub.Messaging.RabbitMQ;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Testcontainers.RabbitMq;
using Xunit;

namespace LogisticsHub.Messaging.RabbitMQ.Tests;

public sealed class RabbitMqIntegrationTests : IAsyncLifetime
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);
    private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder("rabbitmq:3.13-alpine").Build();

    private RabbitMqOptions _options = null!;
    private RabbitMqConnectionProvider _connectionProvider = null!;

    static RabbitMqIntegrationTests()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DOCKER_API_VERSION")))
        {
            Environment.SetEnvironmentVariable("DOCKER_API_VERSION", "1.42");
        }
    }

    public async Task InitializeAsync()
    {
        await _rabbitMqContainer.StartAsync();

        _options = CreateOptions(_rabbitMqContainer.GetConnectionString());
        _connectionProvider = new RabbitMqConnectionProvider(_options);
    }

    public async Task DisposeAsync()
    {
        if (_connectionProvider is not null)
        {
            await _connectionProvider.DisposeAsync();
        }

        await _rabbitMqContainer.DisposeAsync();
    }

    [Fact]
    public async Task PublishAsync_WhenMandatoryMessageIsUnroutable_Throws()
    {
        await using var topology = await CreateTopologyAsync(declareQueue: true, bindQueue: false);
        var publisher = new RabbitMqPublisher(
            _connectionProvider,
            _options,
            NullLogger<RabbitMqPublisher>.Instance);

        await Assert.ThrowsAnyAsync<Exception>(() =>
            publisher.PublishAsync(
                topology.RoutingKey,
                new TestMessage(Guid.NewGuid(), "unroutable")));
    }

    [Fact]
    public async Task PublishAsync_WhenMessageIsRoutable_CanBeConsumed()
    {
        await using var topology = await CreateTopologyAsync(declareQueue: true, bindQueue: true);
        var publisher = new RabbitMqPublisher(
            _connectionProvider,
            _options,
            NullLogger<RabbitMqPublisher>.Instance);

        await publisher.PublishAsync(
            topology.RoutingKey,
            new TestMessage(Guid.NewGuid(), "published"));

        var result = await WaitForBasicGetAsync(topology.QueueName);

        Assert.NotNull(result);
        Assert.Contains("published", Encoding.UTF8.GetString(result.Body.ToArray()));
    }

    [Fact]
    public async Task Consumer_WhenHandlerFailsThroughRetries_SendsMessageToDeadLetterQueue()
    {
        await using var topology = await CreateTopologyAsync(declareQueue: false, bindQueue: false);
        await using var consumer = new FailingConsumer(
            _connectionProvider,
            _options,
            topology.QueueName,
            topology.RoutingKey);

        await consumer.StartAsync(CancellationToken.None);
        await WaitForQueueAsync(topology.QueueName);
        await PublishRawMessageAsync(topology.RoutingKey, new TestMessage(Guid.NewGuid(), "fail"));

        var deadLetter = await WaitForBasicGetAsync(topology.DeadLetterQueueName);

        Assert.NotNull(deadLetter);
        Assert.True(consumer.Attempts >= 3);

        await consumer.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Consumer_UsesPrefetchCountToLimitUnacknowledgedDeliveries()
    {
        _options.ConsumerPrefetchCount = 1;
        await using var topology = await CreateTopologyAsync(declareQueue: false, bindQueue: false);
        var releaseHandler = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var consumer = new BlockingConsumer(
            _connectionProvider,
            _options,
            topology.QueueName,
            topology.RoutingKey,
            releaseHandler);

        await consumer.StartAsync(CancellationToken.None);
        await WaitForQueueAsync(topology.QueueName);
        await PublishRawMessageAsync(topology.RoutingKey, new TestMessage(Guid.NewGuid(), "first"));
        await PublishRawMessageAsync(topology.RoutingKey, new TestMessage(Guid.NewGuid(), "second"));
        await consumer.FirstDeliveryReceived;
        var messageCount = await WaitForQueueMessageCountAsync(topology.QueueName, expectedMessageCount: 1);

        releaseHandler.SetResult();
        await WaitUntilAsync(() => consumer.HandledCount == 2);

        Assert.Equal(1u, messageCount);
        Assert.Equal(1, consumer.MaxConcurrentHandlers);

        await consumer.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Consumer_StopAsync_CancelsConsumptionCleanly()
    {
        await using var topology = await CreateTopologyAsync(declareQueue: false, bindQueue: false);
        var releaseHandler = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var consumer = new BlockingConsumer(
            _connectionProvider,
            _options,
            topology.QueueName,
            topology.RoutingKey,
            releaseHandler);

        await consumer.StartAsync(CancellationToken.None);
        await WaitForQueueAsync(topology.QueueName);
        await PublishRawMessageAsync(topology.RoutingKey, new TestMessage(Guid.NewGuid(), "stop"));
        await consumer.FirstDeliveryReceived;

        var stopTask = consumer.StopAsync(CancellationToken.None);
        var completed = await Task.WhenAny(stopTask, Task.Delay(Timeout));

        Assert.Same(stopTask, completed);
        Assert.True(consumer.ObservedCancellation);
    }

    private async Task<RabbitMqTopology> CreateTopologyAsync(
        bool declareQueue,
        bool bindQueue)
    {
        var uniqueName = $"rabbitmq-test-{Guid.NewGuid():N}";
        var topology = new RabbitMqTopology(
            ExchangeName: uniqueName,
            QueueName: $"{uniqueName}.queue",
            RoutingKey: $"{uniqueName}.routing",
            DeadLetterExchangeName: $"{uniqueName}.dlx",
            DeadLetterQueueName: $"{uniqueName}.queue.dlq");
        _options.ExchangeName = topology.ExchangeName;

        var connection = await _connectionProvider.GetConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        await channel.ExchangeDeclareAsync(topology.ExchangeName, ExchangeType.Topic, durable: true);

        if (declareQueue)
        {
            await channel.QueueDeclareAsync(topology.QueueName, durable: true, exclusive: false, autoDelete: false);

            if (bindQueue)
            {
                await channel.QueueBindAsync(topology.QueueName, topology.ExchangeName, topology.RoutingKey);
            }
        }

        return topology;
    }

    private async Task PublishRawMessageAsync(string routingKey, TestMessage message)
    {
        var connection = await _connectionProvider.GetConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        await channel.BasicPublishAsync(
            exchange: _options.ExchangeName,
            routingKey: routingKey,
            mandatory: true,
            body: body);
    }

    private async Task<BasicGetResult?> WaitForBasicGetAsync(string queueName)
    {
        return await WaitForAsync(() => TryGetMessageAsync(queueName));
    }

    private async Task<uint> WaitForQueueMessageCountAsync(
        string queueName,
        uint expectedMessageCount)
    {
        return await WaitForAsync(() => TryGetQueueMessageCountAsync(queueName, expectedMessageCount));
    }

    private async Task WaitForQueueAsync(string queueName)
    {
        await WaitForAsync(() => TryConfirmQueueExistsAsync(queueName));
    }

    private async Task<BasicGetResult?> TryGetMessageAsync(string queueName)
    {
        var connection = await _connectionProvider.GetConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        return await channel.BasicGetAsync(queueName, autoAck: true);
    }

    private async Task<uint?> TryGetQueueMessageCountAsync(
        string queueName,
        uint expectedMessageCount)
    {
        var connection = await _connectionProvider.GetConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        var declaration = await channel.QueueDeclarePassiveAsync(queueName);
        return declaration.MessageCount == expectedMessageCount
            ? declaration.MessageCount
            : null;
    }

    private async Task<bool?> TryConfirmQueueExistsAsync(string queueName)
    {
        try
        {
            var connection = await _connectionProvider.GetConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();
            await channel.QueueDeclarePassiveAsync(queueName);
            return true;
        }
        catch (OperationInterruptedException)
        {
            return null;
        }
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        await WaitForAsync(() => Task.FromResult(condition() ? true : (bool?)null));
    }

    private static async Task<T> WaitForAsync<T>(Func<Task<T?>> action)
        where T : struct
    {
        using var timeout = new CancellationTokenSource(Timeout);

        while (!timeout.IsCancellationRequested)
        {
            var result = await action();
            if (result.HasValue)
            {
                return result.Value;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), timeout.Token);
        }

        throw new TimeoutException("The expected RabbitMQ condition was not reached.");
    }

    private static async Task<BasicGetResult?> WaitForAsync(Func<Task<BasicGetResult?>> action)
    {
        using var timeout = new CancellationTokenSource(Timeout);

        while (!timeout.IsCancellationRequested)
        {
            var result = await action();
            if (result is not null)
            {
                return result;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), timeout.Token);
        }

        throw new TimeoutException("The expected RabbitMQ message was not received.");
    }

    private static RabbitMqOptions CreateOptions(string connectionString)
    {
        var uri = new Uri(connectionString);
        var credentials = uri.UserInfo.Split(':', 2);

        return new RabbitMqOptions
        {
            HostName = uri.Host,
            Port = uri.Port,
            UserName = Uri.UnescapeDataString(credentials[0]),
            Password = Uri.UnescapeDataString(credentials[1]),
            ExchangeName = $"rabbitmq-test-{Guid.NewGuid():N}",
            ConsumerPrefetchCount = 1
        };
    }

    private sealed record TestMessage(Guid Id, string Value);

    private sealed record RabbitMqTopology(
        string ExchangeName,
        string QueueName,
        string RoutingKey,
        string DeadLetterExchangeName,
        string DeadLetterQueueName) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FailingConsumer : RabbitMqConsumerBackgroundService<TestMessage>, IAsyncDisposable
    {
        public FailingConsumer(
            IRabbitMqConnectionProvider connectionProvider,
            RabbitMqOptions options,
            string queueName,
            string routingKey)
            : base(
                connectionProvider,
                options,
                NullLogger<FailingConsumer>.Instance,
                queueName,
                routingKey)
        {
        }

        public int Attempts { get; private set; }

        public ValueTask DisposeAsync()
        {
            return new ValueTask(StopAsync(CancellationToken.None));
        }

        protected override Task HandleMessageAsync(
            TestMessage message,
            CancellationToken cancellationToken)
        {
            Attempts++;
            throw new InvalidOperationException("handler failed");
        }
    }

    private sealed class BlockingConsumer : RabbitMqConsumerBackgroundService<TestMessage>, IAsyncDisposable
    {
        private readonly TaskCompletionSource _releaseHandler;
        private readonly TaskCompletionSource _firstDeliveryReceived = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _activeHandlers;

        public BlockingConsumer(
            IRabbitMqConnectionProvider connectionProvider,
            RabbitMqOptions options,
            string queueName,
            string routingKey,
            TaskCompletionSource releaseHandler)
            : base(
                connectionProvider,
                options,
                NullLogger<BlockingConsumer>.Instance,
                queueName,
                routingKey)
        {
            _releaseHandler = releaseHandler;
        }

        public Task FirstDeliveryReceived => _firstDeliveryReceived.Task;

        public int HandledCount { get; private set; }

        public int MaxConcurrentHandlers { get; private set; }

        public bool ObservedCancellation { get; private set; }

        public ValueTask DisposeAsync()
        {
            return new ValueTask(StopAsync(CancellationToken.None));
        }

        protected override async Task HandleMessageAsync(
            TestMessage message,
            CancellationToken cancellationToken)
        {
            var activeHandlers = Interlocked.Increment(ref _activeHandlers);
            MaxConcurrentHandlers = Math.Max(MaxConcurrentHandlers, activeHandlers);
            _firstDeliveryReceived.TrySetResult();

            try
            {
                await _releaseHandler.Task.WaitAsync(cancellationToken);
                HandledCount++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                ObservedCancellation = true;
                throw;
            }
            finally
            {
                Interlocked.Decrement(ref _activeHandlers);
            }
        }
    }
}
