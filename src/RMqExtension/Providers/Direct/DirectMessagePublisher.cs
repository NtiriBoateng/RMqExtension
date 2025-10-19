using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RMqExtension.Abstractions;
using RMqExtension.Configuration;
using System.Text;

namespace RMqExtension.Providers.Direct;

/// <summary>
/// Direct RabbitMQ message publisher implementation
/// </summary>
public class DirectMessagePublisher : IMessagePublisher
{
    private readonly DirectConnectionManager _connectionManager;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<DirectMessagePublisher> _logger;

    public DirectMessagePublisher(
        DirectConnectionManager connectionManager,
        IOptions<RabbitMqOptions> options,
        ILogger<DirectMessagePublisher> logger)
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
        if (!_connectionManager.IsConnected)
        {
            await _connectionManager.ConnectAsync(cancellationToken);
        }

        try
        {
            var channel = _connectionManager.GetChannel();
            var messageBody = SerializeMessage(message);
            var properties = CreateBasicProperties(channel, configuration.Properties);

            channel.BasicPublish(
                exchange: configuration.Exchange,
                routingKey: configuration.RoutingKey,
                mandatory: configuration.Mandatory,
                basicProperties: properties,
                body: messageBody);

            _logger.LogDebug("Published message to exchange '{Exchange}' with routing key '{RoutingKey}'",
                configuration.Exchange, configuration.RoutingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to exchange '{Exchange}' with routing key '{RoutingKey}'",
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
            var channel = _connectionManager.GetChannel();
            var batch = channel.CreateBasicPublishBatch();

            foreach (var message in messages)
            {
                var messageBody = SerializeMessage(message);
                var properties = CreateBasicProperties(channel, new MessageProperties());

                batch.Add(exchangeName, routingKey, false, properties, messageBody.AsMemory());
            }

            batch.Publish();

            _logger.LogDebug("Published batch of {MessageCount} messages to exchange '{Exchange}' with routing key '{RoutingKey}'",
                messages.Count(), exchangeName, routingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish batch messages to exchange '{Exchange}' with routing key '{RoutingKey}'",
                exchangeName, routingKey);
            throw;
        }
    }

    private byte[] SerializeMessage<T>(T message)
    {
        try
        {
            var json = JsonConvert.SerializeObject(message, Formatting.None);
            return Encoding.UTF8.GetBytes(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to serialize message of type {MessageType}", typeof(T).Name);
            throw;
        }
    }

    private IBasicProperties CreateBasicProperties(IModel channel, MessageProperties properties)
    {
        var basicProperties = channel.CreateBasicProperties();

        basicProperties.ContentType = properties.ContentType;
        basicProperties.ContentEncoding = properties.ContentEncoding;
        basicProperties.DeliveryMode = properties.DeliveryMode;
        basicProperties.Priority = properties.Priority;

        if (!string.IsNullOrEmpty(properties.CorrelationId))
            basicProperties.CorrelationId = properties.CorrelationId;

        if (!string.IsNullOrEmpty(properties.ReplyTo))
            basicProperties.ReplyTo = properties.ReplyTo;

        if (!string.IsNullOrEmpty(properties.Expiration))
            basicProperties.Expiration = properties.Expiration;

        if (!string.IsNullOrEmpty(properties.MessageId))
            basicProperties.MessageId = properties.MessageId;

        if (properties.Timestamp.HasValue)
            basicProperties.Timestamp = new AmqpTimestamp(properties.Timestamp.Value.ToUnixTimeSeconds());

        if (!string.IsNullOrEmpty(properties.Type))
            basicProperties.Type = properties.Type;

        if (!string.IsNullOrEmpty(properties.UserId))
            basicProperties.UserId = properties.UserId;

        if (!string.IsNullOrEmpty(properties.AppId))
            basicProperties.AppId = properties.AppId;

        if (properties.Headers.Any())
        {
            basicProperties.Headers = new Dictionary<string, object>(properties.Headers);
        }

        return basicProperties;
    }
}