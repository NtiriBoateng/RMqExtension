# Build and Pack Instructions

## Prerequisites
- .NET 6 SDK or higher
- Visual Studio 2022 or VS Code
- RabbitMQ server (for testing)

## Building the Library

### Using Command Line
```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build --configuration Release

# Run tests (if any)
dotnet test

# Create NuGet package
dotnet pack --configuration Release --output ./nupkg
```

### Using Visual Studio
1. Open `RMqExtension.sln` in Visual Studio
2. Set Configuration to `Release`
3. Build -> Build Solution
4. Right-click on the project -> Pack

## Publishing to NuGet

### Local NuGet Feed
```bash
# Add local source (one time setup)
dotnet nuget add source C:\LocalNuGet --name Local

# Push to local feed
dotnet nuget push .\nupkg\RMqExtension.1.0.0.nupkg --source Local
```

### NuGet.org
```bash
# Get API key from nuget.org
# Replace YOUR_API_KEY with actual key
dotnet nuget push .\nupkg\RMqExtension.1.0.0.nupkg --source https://api.nuget.org/v3/index.json --api-key YOUR_API_KEY
```

## Using the Library

### Installation
```bash
dotnet add package RMqExtension
```

### Configuration in appsettings.json
```json
{
  "RabbitMq": {
    "ConnectionString": "amqp://guest:guest@localhost:5672/",
    "DefaultExchange": "my-app-exchange",
    "ImplementationType": "Direct",
    "RetryAttempts": 3,
    "AutomaticRecoveryEnabled": true
  }
}
```

### Basic Setup in Program.cs
```csharp
using RMqExtension.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add RabbitMQ services
builder.Services.AddRabbitMq(builder.Configuration.GetSection("RabbitMq"));

// Or configure directly
builder.Services.AddRabbitMq(options =>
{
    options.ConnectionString = "amqp://guest:guest@localhost:5672/";
    options.DefaultExchange = "my-exchange";
    options.ImplementationType = RabbitMqImplementationType.Direct;
});

var app = builder.Build();
app.Run();
```

## Development Setup

### Running RabbitMQ with Docker
```bash
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

### Testing the Library
```csharp
// Inject and use the services
public class MyService
{
    private readonly IMessagePublisher _publisher;
    
    public MyService(IMessagePublisher publisher)
    {
        _publisher = publisher;
    }
    
    public async Task SendMessage()
    {
        await _publisher.PublishAsync(new { Message = "Hello World" }, "test.routing.key");
    }
}
```