using RabbitMQ.Client;

namespace LogisticsHub.Messaging.RabbitMQ;

public sealed class RabbitMqConnectionProvider : IRabbitMqConnectionProvider
{
    private readonly RabbitMqOptions _options;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private IConnection? connection;

    public RabbitMqConnectionProvider(RabbitMqOptions options)
    {
        _options = options;
    }

    public async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (connection?.IsOpen == true)
        {
            return connection;
        }

        await _connectionLock.WaitAsync(cancellationToken);

        try
        {
            if (connection?.IsOpen == true)
            {
                return connection;
            }

            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password
            };

            connection = await factory.CreateConnectionAsync(cancellationToken);

            return connection;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _connectionLock.Dispose();

        if (connection is not null)
        {
            await connection.DisposeAsync();
        }
    }
}
