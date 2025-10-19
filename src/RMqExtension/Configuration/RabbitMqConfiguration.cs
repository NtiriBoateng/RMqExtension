namespace RMqExtension.Configuration;

/// <summary>
/// Represents the implementation type for RabbitMQ operations
/// </summary>
public enum RabbitMqImplementationType
{
    /// <summary>
    /// Use direct RabbitMQ.Client implementation
    /// </summary>
    Direct,
    
    /// <summary>
    /// Use MassTransit implementation
    /// </summary>
    MassTransit
}

/// <summary>
/// Configuration options for RabbitMQ connection and settings
/// </summary>
public class RabbitMqOptions
{
    /// <summary>
    /// RabbitMQ connection string (e.g., "amqp://guest:guest@localhost:5672/")
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;
    
    /// <summary>
    /// Default exchange name to use for publishing
    /// </summary>
    public string DefaultExchange { get; set; } = string.Empty;
    
    /// <summary>
    /// Default queue name prefix
    /// </summary>
    public string DefaultQueuePrefix { get; set; } = string.Empty;
    
    /// <summary>
    /// Implementation type to use (Direct or MassTransit)
    /// </summary>
    public RabbitMqImplementationType ImplementationType { get; set; } = RabbitMqImplementationType.Direct;
    
    /// <summary>
    /// Connection retry attempts
    /// </summary>
    public int RetryAttempts { get; set; } = 3;
    
    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 30;
    
    /// <summary>
    /// Enable automatic recovery
    /// </summary>
    public bool AutomaticRecoveryEnabled { get; set; } = true;
    
    /// <summary>
    /// Network recovery interval in seconds
    /// </summary>
    public int NetworkRecoveryIntervalSeconds { get; set; } = 10;
    
    /// <summary>
    /// Additional connection properties
    /// </summary>
    public Dictionary<string, object> ConnectionProperties { get; set; } = new();
}

/// <summary>
/// Configuration for exchange settings
/// </summary>
public class ExchangeConfiguration
{
    /// <summary>
    /// Exchange name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Exchange type (direct, topic, fanout, headers)
    /// </summary>
    public string Type { get; set; } = "direct";
    
    /// <summary>
    /// Whether the exchange is durable
    /// </summary>
    public bool Durable { get; set; } = true;
    
    /// <summary>
    /// Whether to auto-delete the exchange
    /// </summary>
    public bool AutoDelete { get; set; } = false;
    
    /// <summary>
    /// Additional exchange arguments
    /// </summary>
    public Dictionary<string, object> Arguments { get; set; } = new();
}

/// <summary>
/// Configuration for queue settings
/// </summary>
public class QueueConfiguration
{
    /// <summary>
    /// Queue name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether the queue is durable
    /// </summary>
    public bool Durable { get; set; } = true;
    
    /// <summary>
    /// Whether the queue is exclusive
    /// </summary>
    public bool Exclusive { get; set; } = false;
    
    /// <summary>
    /// Whether to auto-delete the queue
    /// </summary>
    public bool AutoDelete { get; set; } = false;
    
    /// <summary>
    /// Routing key to bind the queue
    /// </summary>
    public string RoutingKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Exchange to bind the queue to
    /// </summary>
    public string Exchange { get; set; } = string.Empty;
    
    /// <summary>
    /// Additional queue arguments
    /// </summary>
    public Dictionary<string, object> Arguments { get; set; } = new();
}

/// <summary>
/// Configuration for publishing messages
/// </summary>
public class PublishConfiguration
{
    /// <summary>
    /// Exchange to publish to
    /// </summary>
    public string Exchange { get; set; } = string.Empty;
    
    /// <summary>
    /// Routing key for the message
    /// </summary>
    public string RoutingKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether the message is mandatory
    /// </summary>
    public bool Mandatory { get; set; } = false;
    
    /// <summary>
    /// Whether the message is immediate
    /// </summary>
    public bool Immediate { get; set; } = false;
    
    /// <summary>
    /// Message properties
    /// </summary>
    public MessageProperties Properties { get; set; } = new();
}

/// <summary>
/// Message properties for publishing
/// </summary>
public class MessageProperties
{
    /// <summary>
    /// Message content type
    /// </summary>
    public string ContentType { get; set; } = "application/json";
    
    /// <summary>
    /// Message content encoding
    /// </summary>
    public string ContentEncoding { get; set; } = "utf-8";
    
    /// <summary>
    /// Message delivery mode (1 = non-persistent, 2 = persistent)
    /// </summary>
    public byte DeliveryMode { get; set; } = 2;
    
    /// <summary>
    /// Message priority
    /// </summary>
    public byte Priority { get; set; } = 0;
    
    /// <summary>
    /// Message correlation ID
    /// </summary>
    public string? CorrelationId { get; set; }
    
    /// <summary>
    /// Reply-to address
    /// </summary>
    public string? ReplyTo { get; set; }
    
    /// <summary>
    /// Message expiration
    /// </summary>
    public string? Expiration { get; set; }
    
    /// <summary>
    /// Message ID
    /// </summary>
    public string? MessageId { get; set; }
    
    /// <summary>
    /// Message timestamp
    /// </summary>
    public DateTimeOffset? Timestamp { get; set; }
    
    /// <summary>
    /// Message type
    /// </summary>
    public string? Type { get; set; }
    
    /// <summary>
    /// User ID
    /// </summary>
    public string? UserId { get; set; }
    
    /// <summary>
    /// Application ID
    /// </summary>
    public string? AppId { get; set; }
    
    /// <summary>
    /// Additional headers
    /// </summary>
    public Dictionary<string, object> Headers { get; set; } = new();
}