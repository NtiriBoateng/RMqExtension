using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RMqExtension.Abstractions;
using RMqExtension.Configuration;

namespace RMqExtension.Providers.MassTransit;

/// <summary>
/// MassTransit message publisher implementation
/// </summary>
public class MassTransitMessagePublisher : IMessagePublisher
{
    private readonly MassTransitConnectionManager _connectionManager;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<MassTransitMessagePublisher> _logger;

    public MassTransitMessagePublisher(
        MassTransitConnectionManager connectionManager,
        IOptions<RabbitMqOptions> options,
        ILogger<MassTransitMessagePublisher> logger)
    {
        _connectionManager = connectionManager;
        _options = options.Value;
        _logger = logger;
    }

    public async Task PublishAsync<T>(T message, string routingKey, string? exchange = null, CancellationToken cancellationToken = default)
    {
        var config = new PublishConfiguration
        {
            Exchange = exchange ?? _options.DefaultExchange,
            RoutingKey = routingKey
        };

        await PublishAsync(message, config, cancellationToken);
    }

    public async Task PublishAsync<T>(T message, PublishConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        if (!_connectionManager.IsConnected)
        {
            await _connectionManager.ConnectAsync(cancellationToken);
        }

        try
        {
            var bus = _connectionManager.GetBus();
            
            // Create the endpoint URI for the specific exchange and routing key
            var exchangeAddress = new Uri($"exchange:{configuration.Exchange}");
            
            var endpoint = await bus.GetSendEndpoint(exchangeAddress);
            
            await endpoint.Send(message, context =>
            {
                context.SetRoutingKey(configuration.RoutingKey);
                
                // Set message properties
                if (!string.IsNullOrEmpty(configuration.Properties.CorrelationId))
                    context.CorrelationId = Guid.Parse(configuration.Properties.CorrelationId);
                
                if (!string.IsNullOrEmpty(configuration.Properties.MessageId))
                    context.MessageId = Guid.Parse(configuration.Properties.MessageId);
                
                if (configuration.Properties.Timestamp.HasValue)
                {
                    // Note: SentTime is read-only in MassTransit, timestamp is set automatically
                }
                
                if (!string.IsNullOrEmpty(configuration.Properties.Expiration))
                    context.TimeToLive = TimeSpan.Parse(configuration.Properties.Expiration);
                
                // Set headers
                foreach (var header in configuration.Properties.Headers)
                {
                    context.Headers.Set(header.Key, header.Value);
                }
            }, cancellationToken);

            _logger.LogDebug("Published message via MassTransit to exchange '{Exchange}' with routing key '{RoutingKey}'",
                configuration.Exchange, configuration.RoutingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message via MassTransit to exchange '{Exchange}' with routing key '{RoutingKey}'",
                configuration.Exchange, configuration.RoutingKey);
            throw;
        }
    }

    public async Task PublishBatchAsync<T>(IEnumerable<T> messages, string routingKey, string? exchange = null, CancellationToken cancellationToken = default)
    {
        if (!_connectionManager.IsConnected)
        {
            await _connectionManager.ConnectAsync(cancellationToken);
        }

        var exchangeName = exchange ?? _options.DefaultExchange;

        try
        {
            var bus = _connectionManager.GetBus();
            var exchangeAddress = new Uri($"exchange:{exchangeName}");
            var endpoint = await bus.GetSendEndpoint(exchangeAddress);

            // Send messages in parallel for better performance
            var sendTasks = messages.Where(m => m != null).Select(async message =>
            {
                await endpoint.Send(message!, context =>
                {
                    context.SetRoutingKey(routingKey);
                }, cancellationToken);
            });

            await Task.WhenAll(sendTasks);

            _logger.LogDebug("Published batch of {MessageCount} messages via MassTransit to exchange '{Exchange}' with routing key '{RoutingKey}'",
                messages.Count(), exchangeName, routingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish batch messages via MassTransit to exchange '{Exchange}' with routing key '{RoutingKey}'",
                exchangeName, routingKey);
            throw;
        }
    }
}