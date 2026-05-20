using RabbitMQ.Client;

namespace LogisticsHub.Messaging.RabbitMQ;

public sealed class RabbitMqConnectionProvider : IRabbitMqConnectionProvider
{
    private readonly RabbitMqOptions options;
    private readonly SemaphoreSlim connectionLock = new(1, 1);
    private IConnection? connection;

    public RabbitMqConnectionProvider(RabbitMqOptions options)
    {
        this.options = options;
    }

    public async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (connection?.IsOpen == true)
        {
            return connection;
        }

        await connectionLock.WaitAsync(cancellationToken);

        try
        {
            if (connection?.IsOpen == true)
            {
                return connection;
            }

            var factory = new ConnectionFactory
            {
                HostName = options.HostName,
                Port = options.Port,
                UserName = options.UserName,
                Password = options.Password
            };

            connection = await factory.CreateConnectionAsync(cancellationToken);

            return connection;
        }
        finally
        {
            connectionLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        connectionLock.Dispose();

        if (connection is not null)
        {
            await connection.DisposeAsync();
        }
    }
}
