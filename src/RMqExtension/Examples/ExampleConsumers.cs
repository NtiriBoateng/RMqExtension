using Microsoft.Extensions.Logging;
using RMqExtension.Abstractions;

namespace RMqExtension.Examples;

/// <summary>
/// Example consumer for user created events
/// </summary>
public class UserCreatedConsumer : IMessageConsumer<UserCreatedEvent>
{
    private readonly ILogger<UserCreatedConsumer> _logger;

    public UserCreatedConsumer(ILogger<UserCreatedConsumer> logger)
    {
        _logger = logger;
    }

    public async Task ConsumeAsync(UserCreatedEvent message, CancellationToken cancellationToken = default)
    {
        try
        {
            // Process the user created event
            _logger.LogInformation("Processing user created event for user {UserId}: {Name} ({Email})",
                message.UserId, message.Name, message.Email);

            // Simulate some processing work
            await Task.Delay(100, cancellationToken);

            // Your business logic here
            // For example: send welcome email, create user profile, etc.

            _logger.LogInformation("Successfully processed user created event for user {UserId}", message.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process user created event for user {UserId}", message.UserId);
            throw; // Re-throw to let the messaging system handle retries
        }
    }
}

/// <summary>
/// Example consumer for order placed events
/// </summary>
public class OrderPlacedConsumer : IMessageConsumer<OrderPlacedEvent>
{
    private readonly ILogger<OrderPlacedConsumer> _logger;

    public OrderPlacedConsumer(ILogger<OrderPlacedConsumer> logger)
    {
        _logger = logger;
    }

    public async Task ConsumeAsync(OrderPlacedEvent message, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing order placed event for order {OrderId} by user {UserId}. Amount: {Amount:C}",
                message.OrderId, message.UserId, message.Amount);

            // Simulate some processing work
            await Task.Delay(200, cancellationToken);

            // Your business logic here
            // For example: validate inventory, process payment, send confirmation email, etc.
            foreach (var item in message.Items)
            {
                _logger.LogDebug("Order item: {ProductName} x {Quantity} @ {Price:C}",
                    item.ProductName, item.Quantity, item.Price);
            }

            _logger.LogInformation("Successfully processed order placed event for order {OrderId}", message.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process order placed event for order {OrderId}", message.OrderId);
            throw; // Re-throw to let the messaging system handle retries
        }
    }
}