using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RMqExtension.Abstractions;
using RMqExtension.Configuration;

namespace RMqExtension.Providers.Direct;

/// <summary>
/// Direct RabbitMQ queue manager implementation
/// </summary>
public class DirectQueueManager : IQueueManager
{
    private readonly DirectConnectionManager _connectionManager;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<DirectQueueManager> _logger;

    public DirectQueueManager(
        DirectConnectionManager connectionManager,
        IOptions<RabbitMqOptions> options,
        ILogger<DirectQueueManager> logger)
    {
        _connectionManager = connectionManager;
        _options = options.Value;
        _logger = logger;
    }

    public async Task DeclareQueueAsync(QueueConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (!_connectionManager.IsConnected)
        {
            await _connectionManager.ConnectAsync(cancellationToken);
        }

        try
        {
            var channel = _connectionManager.GetChannel();
            
            channel.QueueDeclare(
                queue: configuration.Name,
                durable: configuration.Durable,
                exclusive: configuration.Exclusive,
                autoDelete: configuration.AutoDelete,
                arguments: configuration.Arguments);

            _logger.LogDebug("Declared queue '{QueueName}'", configuration.Name);

            // Bind to exchange if specified
            if (!string.IsNullOrEmpty(configuration.Exchange) && !string.IsNullOrEmpty(configuration.RoutingKey))
            {
                await BindQueueAsync(configuration.Name, configuration.Exchange, configuration.RoutingKey, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to declare queue '{QueueName}'", configuration.Name);
            throw;
        }
    }

    public async Task DeclareExchangeAsync(ExchangeConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (!_connectionManager.IsConnected)
        {
            await _connectionManager.ConnectAsync(cancellationToken);
        }

        try
        {
            var channel = _connectionManager.GetChannel();
            
            channel.ExchangeDeclare(
                exchange: configuration.Name,
                type: configuration.Type,
                durable: configuration.Durable,
                autoDelete: configuration.AutoDelete,
                arguments: configuration.Arguments);

            _logger.LogDebug("Declared exchange '{ExchangeName}' of type '{ExchangeType}'", 
                configuration.Name, configuration.Type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to declare exchange '{ExchangeName}'", configuration.Name);
            throw;
        }
    }

    public async Task BindQueueAsync(string queueName, string exchangeName, string routingKey, CancellationToken cancellationToken = default)
    {
        if (!_connectionManager.IsConnected)
        {
            await _connectionManager.ConnectAsync(cancellationToken);
        }

        try
        {
            var channel = _connectionManager.GetChannel();
            
            channel.QueueBind(
                queue: queueName,
                exchange: exchangeName,
                routingKey: routingKey);

            _logger.LogDebug("Bound queue '{QueueName}' to exchange '{ExchangeName}' with routing key '{RoutingKey}'",
                queueName, exchangeName, routingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bind queue '{QueueName}' to exchange '{ExchangeName}' with routing key '{RoutingKey}'",
                queueName, exchangeName, routingKey);
            throw;
        }
    }

    public async Task UnbindQueueAsync(string queueName, string exchangeName, string routingKey, CancellationToken cancellationToken = default)
    {
        if (!_connectionManager.IsConnected)
        {
            await _connectionManager.ConnectAsync(cancellationToken);
        }

        try
        {
            var channel = _connectionManager.GetChannel();
            
            channel.QueueUnbind(
                queue: queueName,
                exchange: exchangeName,
                routingKey: routingKey);

            _logger.LogDebug("Unbound queue '{QueueName}' from exchange '{ExchangeName}' with routing key '{RoutingKey}'",
                queueName, exchangeName, routingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unbind queue '{QueueName}' from exchange '{ExchangeName}' with routing key '{RoutingKey}'",
                queueName, exchangeName, routingKey);
            throw;
        }
    }

    public async Task DeleteQueueAsync(string queueName, bool ifUnused = false, bool ifEmpty = false, CancellationToken cancellationToken = default)
    {
        if (!_connectionManager.IsConnected)
        {
            await _connectionManager.ConnectAsync(cancellationToken);
        }

        try
        {
            var channel = _connectionManager.GetChannel();
            
            channel.QueueDelete(
                queue: queueName,
                ifUnused: ifUnused,
                ifEmpty: ifEmpty);

            _logger.LogDebug("Deleted queue '{QueueName}'", queueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete queue '{QueueName}'", queueName);
            throw;
        }
    }

    public async Task DeleteExchangeAsync(string exchangeName, bool ifUnused = false, CancellationToken cancellationToken = default)
    {
        if (!_connectionManager.IsConnected)
        {
            await _connectionManager.ConnectAsync(cancellationToken);
        }

        try
        {
            var channel = _connectionManager.GetChannel();
            
            channel.ExchangeDelete(
                exchange: exchangeName,
                ifUnused: ifUnused);

            _logger.LogDebug("Deleted exchange '{ExchangeName}'", exchangeName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete exchange '{ExchangeName}'", exchangeName);
            throw;
        }
    }

    public async Task<QueueInfo> GetQueueInfoAsync(string queueName, CancellationToken cancellationToken = default)
    {
        if (!_connectionManager.IsConnected)
        {
            await _connectionManager.ConnectAsync(cancellationToken);
        }

        try
        {
            var channel = _connectionManager.GetChannel();
            
            var result = channel.QueueDeclarePassive(queueName);

            return new QueueInfo
            {
                Name = queueName,
                MessageCount = result.MessageCount,
                ConsumerCount = result.ConsumerCount,
                Durable = true, // QueueDeclarePassive doesn't return this info
                Exclusive = false,
                AutoDelete = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get info for queue '{QueueName}'", queueName);
            throw;
        }
    }
}