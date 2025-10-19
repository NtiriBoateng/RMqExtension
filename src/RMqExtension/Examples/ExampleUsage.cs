using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RMqExtension.Abstractions;
using RMqExtension.Extensions;

namespace RMqExtension.Examples;

/// <summary>
/// Example usage of the RMqExtension library
/// </summary>
public class ExampleUsage
{
    /// <summary>
    /// Example of how to configure services in Program.cs or Startup.cs
    /// </summary>
    public static void ConfigureServices(IServiceCollection services)
    {
        // Option 1: Configure with action delegate
        services.AddRabbitMq(options =>
        {
            options.ConnectionString = "amqp://guest:guest@localhost:5672/";
            options.DefaultExchange = "my-app-exchange";
            options.ImplementationType = Configuration.RabbitMqImplementationType.Direct; // or MassTransit
            options.RetryAttempts = 3;
            options.AutomaticRecoveryEnabled = true;
        });

        // Option 2: Use specific implementation
        services.AddRabbitMqDirect(options =>
        {
            options.ConnectionString = "amqp://guest:guest@localhost:5672/";
            options.DefaultExchange = "my-app-exchange";
        });

        // Option 3: Use MassTransit implementation
        services.AddRabbitMqMassTransit(options =>
        {
            options.ConnectionString = "amqp://guest:guest@localhost:5672/";
            options.DefaultExchange = "my-app-exchange";
        });

        // Register message consumers
        services.AddMessageConsumer<UserCreatedEvent, UserCreatedConsumer>();
        services.AddMessageConsumer<OrderPlacedEvent, OrderPlacedConsumer>();
    }

    /// <summary>
    /// Example of publishing messages
    /// </summary>
    public class MessagePublishingExample
    {
        private readonly IMessagePublisher _publisher;
        private readonly ILogger<MessagePublishingExample> _logger;

        public MessagePublishingExample(IMessagePublisher publisher, ILogger<MessagePublishingExample> logger)
        {
            _publisher = publisher;
            _logger = logger;
        }

        public async Task PublishUserCreatedEvent()
        {
            var userEvent = new UserCreatedEvent
            {
                UserId = Guid.NewGuid(),
                Name = "John Doe",
                Email = "john.doe@example.com"
            };

            // Simple publish with routing key
            await _publisher.PublishAsync(userEvent, "user.created");

            // Publish to specific exchange
            await _publisher.PublishAsync(userEvent, "user.created", "user-events-exchange");

            _logger.LogInformation("Published user created event for {UserId}", userEvent.UserId);
        }

        public async Task PublishOrderPlacedEvent()
        {
            var orderEvent = new OrderPlacedEvent
            {
                OrderId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Amount = 99.99m,
                Items = new List<OrderItem>
                {
                    new() { ProductName = "Product A", Quantity = 2, Price = 29.99m },
                    new() { ProductName = "Product B", Quantity = 1, Price = 39.99m }
                }
            };

            await _publisher.PublishAsync(orderEvent, "order.placed");
            _logger.LogInformation("Published order placed event for {OrderId}", orderEvent.OrderId);
        }

        public async Task PublishBatchMessages()
        {
            var events = new List<UserCreatedEvent>();
            for (int i = 0; i < 10; i++)
            {
                events.Add(new UserCreatedEvent
                {
                    UserId = Guid.NewGuid(),
                    Name = $"User {i}",
                    Email = $"user{i}@example.com"
                });
            }

            await _publisher.PublishBatchAsync(events, "user.created");
            _logger.LogInformation("Published batch of {Count} user created events", events.Count);
        }
    }

    /// <summary>
    /// Example of setting up consumers
    /// </summary>
    public class ConsumerSetupExample
    {
        private readonly IConsumerManager _consumerManager;
        private readonly IQueueManager _queueManager;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ConsumerSetupExample> _logger;

        public ConsumerSetupExample(
            IConsumerManager consumerManager,
            IQueueManager queueManager,
            IServiceProvider serviceProvider,
            ILogger<ConsumerSetupExample> logger)
        {
            _consumerManager = consumerManager;
            _queueManager = queueManager;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task SetupQueuesAndConsumers()
        {
            // Declare exchanges
            await _queueManager.DeclareExchangeAsync(new Configuration.ExchangeConfiguration
            {
                Name = "user-events-exchange",
                Type = "topic",
                Durable = true
            });

            await _queueManager.DeclareExchangeAsync(new Configuration.ExchangeConfiguration
            {
                Name = "order-events-exchange",
                Type = "topic", 
                Durable = true
            });

            // Declare queues
            await _queueManager.DeclareQueueAsync(new Configuration.QueueConfiguration
            {
                Name = "user-created-queue",
                Durable = true,
                Exchange = "user-events-exchange",
                RoutingKey = "user.created"
            });

            await _queueManager.DeclareQueueAsync(new Configuration.QueueConfiguration
            {
                Name = "order-placed-queue",
                Durable = true,
                Exchange = "order-events-exchange",
                RoutingKey = "order.placed"
            });

            // Start consumers with registered consumer instances
            var userConsumer = _serviceProvider.GetRequiredService<IMessageConsumer<UserCreatedEvent>>();
            await _consumerManager.StartConsumingAsync("user-created-queue", userConsumer);

            var orderConsumer = _serviceProvider.GetRequiredService<IMessageConsumer<OrderPlacedEvent>>();
            await _consumerManager.StartConsumingAsync("order-placed-queue", orderConsumer);

            // Alternative: Start consumer with delegate
            await _consumerManager.StartConsumingAsync<UserCreatedEvent>("user-created-queue", async (message, ct) =>
            {
                _logger.LogInformation("Processing user in delegate: {UserId}", message.UserId);
                // Custom processing logic here
            });

            _logger.LogInformation("All consumers started successfully");
        }
    }
}