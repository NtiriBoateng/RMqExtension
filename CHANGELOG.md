# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-10-19

### Added
- Initial release of RMqExtension library
- Support for both direct RabbitMQ.Client and MassTransit implementations
- Generic message publishing with `IMessagePublisher`
- Generic message consuming with `IMessageConsumer<T>` and `IConsumerManager`
- Queue and exchange management with `IQueueManager`
- Connection management with `IConnectionManager`
- Comprehensive configuration options via `RabbitMqOptions`
- Dependency injection extensions for easy setup
- Automatic connection management via hosted service
- Support for batch message publishing
- Configurable retry policies and error handling
- Message properties and headers support
- Exchange and queue binding capabilities
- Example usage and consumer implementations
- Comprehensive logging throughout the library
- Support for .NET 6 and above

### Features
- **Dual Implementation Support**: Choose between RabbitMQ.Client (direct) or MassTransit
- **Generic Messaging**: Type-safe message publishing and consuming
- **Flexible Configuration**: Connection strings, exchanges, routing keys, and more
- **DI Integration**: Seamless integration with .NET dependency injection
- **Error Handling**: Built-in retry policies and comprehensive error handling
- **Connection Management**: Automatic connection recovery and lifecycle management
- **Queue Management**: Create, bind, and manage queues and exchanges
- **Batch Operations**: Efficient batch message publishing
- **Examples**: Complete usage examples and sample consumers

### Dependencies
- Microsoft.Extensions.DependencyInjection.Abstractions >= 6.0.0
- Microsoft.Extensions.Hosting.Abstractions >= 6.0.0
- Microsoft.Extensions.Logging.Abstractions >= 6.0.0
- Microsoft.Extensions.Options >= 6.0.0
- RabbitMQ.Client >= 6.8.1
- MassTransit >= 8.1.3
- MassTransit.RabbitMQ >= 8.1.3
- Newtonsoft.Json >= 13.0.3