using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RMqExtension.Abstractions;
using RMqExtension.Configuration;
using System.Collections.Concurrent;
using System.Text;

namespace RMqExtension.Providers.Direct;

/// <summary>
/// Direct RabbitMQ consumer manager implementation
/// </summary>
public class DirectConsumerManager : IConsumerManager
{
    private readonly DirectConnectionManager _connectionManager;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<DirectConsumerManager> _logger;
    private readonly ConcurrentDictionary<string, ConsumerInfo> _activeConsumers = new();

    public DirectConsumerManager(
        DirectConnectionManager connectionManager,
        IOptions<RabbitMqOptions> options,
        ILogger<DirectConsumerManager> logger)
    {
        _connectionManager = connectionManager;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartConsumingAsync<T>(string queueName, IMessageConsumer<T> consumer, CancellationToken cancellationToken = default)
        where T : class
    {
        await StartConsumingAsync<T>(queueName, consumer.ConsumeAsync, cancellationToken);
    }

    public async Task StartConsumingAsync<T>(string queueName, Func<T, CancellationToken, Task> consumeHandler, CancellationToken cancellationToken = default)
        where T : class
    {
        if (_activeConsumers.ContainsKey(queueName))
        {
            _logger.LogWarning("Consumer for queue '{QueueName}' is already active", queueName);
            return;
        }

        if (!_connectionManager.IsConnected)
        {
            await _connectionManager.ConnectAsync(cancellationToken);
        }

        try
        {
            var channel = _connectionManager.GetChannel();
            var eventingBasicConsumer = new EventingBasicConsumer(channel);

            eventingBasicConsumer.Received += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var messageJson = Encoding.UTF8.GetString(body);
                    var message = JsonConvert.DeserializeObject<T>(messageJson);

                    if (message != null)
                    {
                        await consumeHandler(message, cancellationToken);
                        channel.BasicAck(ea.DeliveryTag, false);
                        
                        _logger.LogDebug("Successfully processed message from queue '{QueueName}'", queueName);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to deserialize message from queue '{QueueName}'. Message will be rejected.", queueName);
                        channel.BasicReject(ea.DeliveryTag, false);
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to deserialize message from queue '{QueueName}'. Message will be rejected.", queueName);
                    channel.BasicReject(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message from queue '{QueueName}'. Message will be rejected.", queueName);
                    channel.BasicReject(ea.DeliveryTag, false);
                }
            };

            var consumerTag = channel.BasicConsume(
                queue: queueName,
                autoAck: false,
                consumer: eventingBasicConsumer);

            var consumerInfo = new ConsumerInfo
            {
                Channel = channel,
                Consumer = eventingBasicConsumer,
                ConsumerTag = consumerTag,
                QueueName = queueName
            };

            _activeConsumers.TryAdd(queueName, consumerInfo);
            
            _logger.LogInformation("Started consuming messages from queue '{QueueName}' with consumer tag '{ConsumerTag}'", 
                queueName, consumerTag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start consuming from queue '{QueueName}'", queueName);
            throw;
        }
    }

    public async Task StopConsumingAsync(string queueName, CancellationToken cancellationToken = default)
    {
        if (!_activeConsumers.TryRemove(queueName, out var consumerInfo))
        {
            _logger.LogWarning("No active consumer found for queue '{QueueName}'", queueName);
            return;
        }

        try
        {
            if (consumerInfo.Channel.IsOpen)
            {
                consumerInfo.Channel.BasicCancel(consumerInfo.ConsumerTag);
            }

            _logger.LogInformation("Stopped consuming from queue '{QueueName}' with consumer tag '{ConsumerTag}'",
                queueName, consumerInfo.ConsumerTag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping consumer for queue '{QueueName}'", queueName);
        }

        await Task.CompletedTask;
    }

    public async Task StopAllConsumersAsync(CancellationToken cancellationToken = default)
    {
        var queueNames = _activeConsumers.Keys.ToList();
        
        foreach (var queueName in queueNames)
        {
            await StopConsumingAsync(queueName, cancellationToken);
        }

        _logger.LogInformation("Stopped all active consumers");
    }

    private class ConsumerInfo
    {
        public IModel Channel { get; set; } = null!;
        public EventingBasicConsumer Consumer { get; set; } = null!;
        public string ConsumerTag { get; set; } = string.Empty;
        public string QueueName { get; set; } = string.Empty;
    }
}