# RMqExtension

A generic RabbitMQ extension library that provides a simple and unified interface for publishing, consuming, and managing queues with RabbitMQ. Supports both direct RabbitMQ.Client and MassTransit implementations.

## Features

- **Generic Message Support**: Send and receive any type of message
- **Multiple Implementation Options**: Choose between direct RabbitMQ.Client or MassTransit
- **Simple Configuration**: Easy setup with connection strings and routing configurations
- **Dependency Injection Support**: Seamless integration with .NET DI container
- **Flexible Routing**: Support for exchanges, routing keys, and channels
- **Error Handling**: Built-in retry policies and error handling

## Installation

```bash
dotnet add package RMqExtension
```

## Quick Start

### Configuration

```csharp
services.AddRabbitMq(options =>
{
    options.ConnectionString = "amqp://guest:guest@localhost:5672/";
    options.DefaultExchange = "my-exchange";
    options.ImplementationType = RabbitMqImplementationType.Direct; // or MassTransit
});
```

### Publishing Messages

```csharp
public class MyService
{
    private readonly IMessagePublisher _publisher;
    
    public MyService(IMessagePublisher publisher)
    {
        _publisher = publisher;
    }
    
    public async Task SendMessage()
    {
        var message = new { Name = "John", Age = 30 };
        await _publisher.PublishAsync(message, "user.created");
    }
}
```

### Consuming Messages

```csharp
public class UserCreatedConsumer : IMessageConsumer<UserCreatedEvent>
{
    public async Task ConsumeAsync(UserCreatedEvent message, CancellationToken cancellationToken)
    {
        // Process the message
        Console.WriteLine($"User created: {message.Name}");
    }
}
```

## License

MIT License
