using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RMqExtension.Abstractions;
using RMqExtension.Configuration;
using System.Collections.Concurrent;

namespace RMqExtension.Providers.MassTransit;

/// <summary>
/// MassTransit consumer manager implementation
/// </summary>
public class MassTransitConsumerManager : IConsumerManager
{
    private readonly MassTransitConnectionManager _connectionManager;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<MassTransitConsumerManager> _logger;
    private readonly ConcurrentDictionary<string, IBusControl> _queueBuses = new();

    public MassTransitConsumerManager(
        MassTransitConnectionManager connectionManager,
        IOptions<RabbitMqOptions> options,
        ILogger<MassTransitConsumerManager> logger)
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
        if (_queueBuses.ContainsKey(queueName))
        {
            _logger.LogWarning("Consumer for queue '{QueueName}' is already active", queueName);
            return;
        }

        try
        {
            // Create a dedicated bus for this consumer
            var busControl = Bus.Factory.CreateUsingRabbitMq(cfg =>
            {
                cfg.Host(new Uri(_options.ConnectionString));

                cfg.ReceiveEndpoint(queueName, ep =>
                {
                    ep.Handler<T>(async context =>
                    {
                        try
                        {
                            await consumeHandler(context.Message, cancellationToken);
                            _logger.LogDebug("Successfully processed message from queue '{QueueName}' via MassTransit", queueName);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing message from queue '{QueueName}' via MassTransit", queueName);
                            throw; // Let MassTransit handle the retry/error handling
                        }
                    });

                    // Configure endpoint settings
                    ep.Durable = true;
                    ep.PrefetchCount = 10; // Configure as needed
                });

                // Configure retry policy
                cfg.UseMessageRetry(r => r.Interval(_options.RetryAttempts, TimeSpan.FromSeconds(5)));
            });

            await busControl.StartAsync(cancellationToken);
            _queueBuses.TryAdd(queueName, busControl);
            
            _logger.LogInformation("Started consuming messages from queue '{QueueName}' via MassTransit", queueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start consuming from queue '{QueueName}' via MassTransit", queueName);
            throw;
        }
    }

    public async Task StopConsumingAsync(string queueName, CancellationToken cancellationToken = default)
    {
        if (!_queueBuses.TryRemove(queueName, out var busControl))
        {
            _logger.LogWarning("No active consumer found for queue '{QueueName}'", queueName);
            return;
        }

        try
        {
            await busControl.StopAsync(cancellationToken);
            
            _logger.LogInformation("Stopped consuming from queue '{QueueName}' via MassTransit", queueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping consumer for queue '{QueueName}' via MassTransit", queueName);
        }
    }

    public async Task StopAllConsumersAsync(CancellationToken cancellationToken = default)
    {
        var queueNames = _queueBuses.Keys.ToList();
        
        var stopTasks = queueNames.Select(queueName => StopConsumingAsync(queueName, cancellationToken));
        await Task.WhenAll(stopTasks);

        _logger.LogInformation("Stopped all active consumers via MassTransit");
    }
}