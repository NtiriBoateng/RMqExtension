using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RMqExtension.Abstractions;
using RMqExtension.Configuration;

namespace RMqExtension.Providers.MassTransit;

/// <summary>
/// MassTransit queue manager implementation
/// </summary>
public class MassTransitQueueManager : IQueueManager
{
    private readonly MassTransitConnectionManager _connectionManager;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<MassTransitQueueManager> _logger;

    public MassTransitQueueManager(
        MassTransitConnectionManager connectionManager,
        IOptions<RabbitMqOptions> options,
        ILogger<MassTransitQueueManager> logger)
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
            var bus = _connectionManager.GetBus();
            
            // MassTransit typically handles queue declaration automatically
            // But we can use the management API if needed
            _logger.LogDebug("Queue '{QueueName}' will be declared automatically by MassTransit when first used", configuration.Name);
            
            // Note: In MassTransit, queues are typically declared through consumer configuration
            // rather than explicit declaration. This method serves as a placeholder for consistency.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to declare queue '{QueueName}' via MassTransit", configuration.Name);
            throw;
        }

        await Task.CompletedTask;
    }

    public async Task DeclareExchangeAsync(ExchangeConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (!_connectionManager.IsConnected)
        {
            await _connectionManager.ConnectAsync(cancellationToken);
        }

        try
        {
            // MassTransit handles exchange declaration automatically
            _logger.LogDebug("Exchange '{ExchangeName}' will be declared automatically by MassTransit when first used", configuration.Name);
            
            // Note: In MassTransit, exchanges are typically declared automatically
            // This method serves as a placeholder for consistency with the direct implementation.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to declare exchange '{ExchangeName}' via MassTransit", configuration.Name);
            throw;
        }

        await Task.CompletedTask;
    }

    public async Task BindQueueAsync(string queueName, string exchangeName, string routingKey, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Queue binding in MassTransit is handled through consumer configuration");
        await Task.CompletedTask;
    }

    public async Task UnbindQueueAsync(string queueName, string exchangeName, string routingKey, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Queue unbinding in MassTransit is handled through consumer configuration");
        await Task.CompletedTask;
    }

    public async Task DeleteQueueAsync(string queueName, bool ifUnused = false, bool ifEmpty = false, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Queue deletion is not directly supported in MassTransit. Use RabbitMQ management tools for this operation.");
        await Task.CompletedTask;
    }

    public async Task DeleteExchangeAsync(string exchangeName, bool ifUnused = false, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Exchange deletion is not directly supported in MassTransit. Use RabbitMQ management tools for this operation.");
        await Task.CompletedTask;
    }

    public async Task<QueueInfo> GetQueueInfoAsync(string queueName, CancellationToken cancellationToken = default)
    {
        // MassTransit doesn't provide direct access to queue information
        // This would require using the RabbitMQ management API or the direct client
        _logger.LogWarning("Queue information retrieval is not directly supported in MassTransit. Consider using the direct RabbitMQ implementation or management API.");
        
        return new QueueInfo
        {
            Name = queueName,
            MessageCount = 0,
            ConsumerCount = 0,
            Durable = true,
            Exclusive = false,
            AutoDelete = false
        };
    }
}