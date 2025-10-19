using RMqExtension.Configuration;

namespace RMqExtension.Abstractions;

/// <summary>
/// Interface for publishing messages to RabbitMQ
/// </summary>
public interface IMessagePublisher
{
    /// <summary>
    /// Publishes a message asynchronously
    /// </summary>
    /// <typeparam name="T">Type of the message</typeparam>
    /// <param name="message">The message to publish</param>
    /// <param name="routingKey">Routing key for the message</param>
    /// <param name="exchange">Exchange to publish to (optional, uses default if not specified)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    Task PublishAsync<T>(T message, string routingKey, string? exchange = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Publishes a message asynchronously with detailed configuration
    /// </summary>
    /// <typeparam name="T">Type of the message</typeparam>
    /// <param name="message">The message to publish</param>
    /// <param name="configuration">Publishing configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    Task PublishAsync<T>(T message, PublishConfiguration configuration, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Publishes multiple messages asynchronously
    /// </summary>
    /// <typeparam name="T">Type of the messages</typeparam>
    /// <param name="messages">The messages to publish</param>
    /// <param name="routingKey">Routing key for the messages</param>
    /// <param name="exchange">Exchange to publish to (optional, uses default if not specified)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    Task PublishBatchAsync<T>(IEnumerable<T> messages, string routingKey, string? exchange = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for consuming messages from RabbitMQ
/// </summary>
/// <typeparam name="T">Type of message to consume</typeparam>
public interface IMessageConsumer<T>
{
    /// <summary>
    /// Consumes a message asynchronously
    /// </summary>
    /// <param name="message">The received message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    Task ConsumeAsync(T message, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for managing RabbitMQ queues
/// </summary>
public interface IQueueManager
{
    /// <summary>
    /// Declares a queue
    /// </summary>
    /// <param name="configuration">Queue configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    Task DeclareQueueAsync(QueueConfiguration configuration, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Declares an exchange
    /// </summary>
    /// <param name="configuration">Exchange configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    Task DeclareExchangeAsync(ExchangeConfiguration configuration, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Binds a queue to an exchange
    /// </summary>
    /// <param name="queueName">Name of the queue</param>
    /// <param name="exchangeName">Name of the exchange</param>
    /// <param name="routingKey">Routing key for binding</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    Task BindQueueAsync(string queueName, string exchangeName, string routingKey, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Unbinds a queue from an exchange
    /// </summary>
    /// <param name="queueName">Name of the queue</param>
    /// <param name="exchangeName">Name of the exchange</param>
    /// <param name="routingKey">Routing key for unbinding</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    Task UnbindQueueAsync(string queueName, string exchangeName, string routingKey, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes a queue
    /// </summary>
    /// <param name="queueName">Name of the queue to delete</param>
    /// <param name="ifUnused">Only delete if unused</param>
    /// <param name="ifEmpty">Only delete if empty</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    Task DeleteQueueAsync(string queueName, bool ifUnused = false, bool ifEmpty = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes an exchange
    /// </summary>
    /// <param name="exchangeName">Name of the exchange to delete</param>
    /// <param name="ifUnused">Only delete if unused</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    Task DeleteExchangeAsync(string exchangeName, bool ifUnused = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets queue information
    /// </summary>
    /// <param name="queueName">Name of the queue</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Queue information</returns>
    Task<QueueInfo> GetQueueInfoAsync(string queueName, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for starting and stopping message consumers
/// </summary>
public interface IConsumerManager
{
    /// <summary>
    /// Starts consuming messages for a specific queue and message type
    /// </summary>
    /// <typeparam name="T">Type of message to consume</typeparam>
    /// <param name="queueName">Name of the queue to consume from</param>
    /// <param name="consumer">Consumer implementation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    Task StartConsumingAsync<T>(string queueName, IMessageConsumer<T> consumer, CancellationToken cancellationToken = default)
        where T : class;
    
    /// <summary>
    /// Starts consuming messages for a specific queue with a delegate
    /// </summary>
    /// <typeparam name="T">Type of message to consume</typeparam>
    /// <param name="queueName">Name of the queue to consume from</param>
    /// <param name="consumeHandler">Handler function for processing messages</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    Task StartConsumingAsync<T>(string queueName, Func<T, CancellationToken, Task> consumeHandler, CancellationToken cancellationToken = default)
        where T : class;
    
    /// <summary>
    /// Stops consuming messages for a specific queue
    /// </summary>
    /// <param name="queueName">Name of the queue to stop consuming from</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    Task StopConsumingAsync(string queueName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Stops all active consumers
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    Task StopAllConsumersAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for connection management
/// </summary>
public interface IConnectionManager : IDisposable
{
    /// <summary>
    /// Indicates whether the connection is open
    /// </summary>
    bool IsConnected { get; }
    
    /// <summary>
    /// Establishes connection to RabbitMQ
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    Task ConnectAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Disconnects from RabbitMQ
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Event fired when connection is established
    /// </summary>
    event EventHandler? Connected;
    
    /// <summary>
    /// Event fired when connection is lost
    /// </summary>
    event EventHandler? Disconnected;
    
    /// <summary>
    /// Event fired when connection recovery starts
    /// </summary>
    event EventHandler? Recovering;
    
    /// <summary>
    /// Event fired when connection is recovered
    /// </summary>
    event EventHandler? Recovered;
}

/// <summary>
/// Queue information
/// </summary>
public class QueueInfo
{
    /// <summary>
    /// Queue name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Number of messages in the queue
    /// </summary>
    public uint MessageCount { get; set; }
    
    /// <summary>
    /// Number of consumers
    /// </summary>
    public uint ConsumerCount { get; set; }
    
    /// <summary>
    /// Whether the queue is durable
    /// </summary>
    public bool Durable { get; set; }
    
    /// <summary>
    /// Whether the queue is exclusive
    /// </summary>
    public bool Exclusive { get; set; }
    
    /// <summary>
    /// Whether the queue auto-deletes
    /// </summary>
    public bool AutoDelete { get; set; }
}