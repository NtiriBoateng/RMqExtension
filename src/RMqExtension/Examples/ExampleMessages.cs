namespace RMqExtension.Examples;

/// <summary>
/// Example message class for demonstration
/// </summary>
public class UserCreatedEvent
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Example message class for orders
/// </summary>
public class OrderPlacedEvent
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public List<OrderItem> Items { get; set; } = new();
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
}

public class OrderItem
{
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}