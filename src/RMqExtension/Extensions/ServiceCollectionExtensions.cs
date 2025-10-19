using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RMqExtension.Abstractions;
using RMqExtension.Configuration;
using RMqExtension.Providers.Direct;
using RMqExtension.Providers.MassTransit;

namespace RMqExtension.Extensions;

/// <summary>
/// Extension methods for registering RabbitMQ services in the dependency injection container
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds RabbitMQ services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Configuration action for RabbitMQ options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddRabbitMq(this IServiceCollection services, Action<RabbitMqOptions> configureOptions)
    {
        if (configureOptions == null)
            throw new ArgumentNullException(nameof(configureOptions));

        services.Configure(configureOptions);
        return services.AddRabbitMqCore();
    }

    /// <summary>
    /// Adds RabbitMQ services to the service collection with configuration section
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration section</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddRabbitMq(this IServiceCollection services, Microsoft.Extensions.Configuration.IConfigurationSection configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        services.Configure<RabbitMqOptions>(configuration);
        return services.AddRabbitMqCore();
    }

    /// <summary>
    /// Adds RabbitMQ services using the direct RabbitMQ.Client implementation
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Configuration action for RabbitMQ options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddRabbitMqDirect(this IServiceCollection services, Action<RabbitMqOptions> configureOptions)
    {
        services.Configure<RabbitMqOptions>(options =>
        {
            configureOptions(options);
            options.ImplementationType = RabbitMqImplementationType.Direct;
        });
        
        return services.AddRabbitMqCore();
    }

    /// <summary>
    /// Adds RabbitMQ services using the MassTransit implementation
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Configuration action for RabbitMQ options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddRabbitMqMassTransit(this IServiceCollection services, Action<RabbitMqOptions> configureOptions)
    {
        services.Configure<RabbitMqOptions>(options =>
        {
            configureOptions(options);
            options.ImplementationType = RabbitMqImplementationType.MassTransit;
        });
        
        return services.AddRabbitMqCore();
    }

    private static IServiceCollection AddRabbitMqCore(this IServiceCollection services)
    {
        // Register the factory that creates the appropriate implementation based on configuration
        services.TryAddSingleton<IConnectionManager>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<RabbitMqOptions>>();
            
            return options.Value.ImplementationType switch
            {
                RabbitMqImplementationType.Direct => serviceProvider.GetRequiredService<DirectConnectionManager>(),
                RabbitMqImplementationType.MassTransit => serviceProvider.GetRequiredService<MassTransitConnectionManager>(),
                _ => throw new InvalidOperationException($"Unsupported implementation type: {options.Value.ImplementationType}")
            };
        });

        services.TryAddSingleton<IMessagePublisher>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<RabbitMqOptions>>();
            
            return options.Value.ImplementationType switch
            {
                RabbitMqImplementationType.Direct => serviceProvider.GetRequiredService<DirectMessagePublisher>(),
                RabbitMqImplementationType.MassTransit => serviceProvider.GetRequiredService<MassTransitMessagePublisher>(),
                _ => throw new InvalidOperationException($"Unsupported implementation type: {options.Value.ImplementationType}")
            };
        });

        services.TryAddSingleton<IQueueManager>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<RabbitMqOptions>>();
            
            return options.Value.ImplementationType switch
            {
                RabbitMqImplementationType.Direct => serviceProvider.GetRequiredService<DirectQueueManager>(),
                RabbitMqImplementationType.MassTransit => serviceProvider.GetRequiredService<MassTransitQueueManager>(),
                _ => throw new InvalidOperationException($"Unsupported implementation type: {options.Value.ImplementationType}")
            };
        });

        services.TryAddSingleton<IConsumerManager>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<RabbitMqOptions>>();
            
            return options.Value.ImplementationType switch
            {
                RabbitMqImplementationType.Direct => serviceProvider.GetRequiredService<DirectConsumerManager>(),
                RabbitMqImplementationType.MassTransit => serviceProvider.GetRequiredService<MassTransitConsumerManager>(),
                _ => throw new InvalidOperationException($"Unsupported implementation type: {options.Value.ImplementationType}")
            };
        });

        // Register concrete implementations
        services.TryAddSingleton<DirectConnectionManager>();
        services.TryAddSingleton<DirectMessagePublisher>();
        services.TryAddSingleton<DirectQueueManager>();
        services.TryAddSingleton<DirectConsumerManager>();

        services.TryAddSingleton<MassTransitConnectionManager>();
        services.TryAddSingleton<MassTransitMessagePublisher>();
        services.TryAddSingleton<MassTransitQueueManager>();
        services.TryAddSingleton<MassTransitConsumerManager>();

        // Register the hosted service for connection management
        services.TryAddSingleton<IHostedService, RabbitMqHostedService>();

        return services;
    }

    /// <summary>
    /// Adds a message consumer to the service collection
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    /// <typeparam name="TConsumer">The consumer type</typeparam>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddMessageConsumer<TMessage, TConsumer>(this IServiceCollection services)
        where TConsumer : class, IMessageConsumer<TMessage>
    {
        services.TryAddScoped<IMessageConsumer<TMessage>, TConsumer>();
        return services;
    }

    /// <summary>
    /// Adds a transient message consumer to the service collection
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    /// <typeparam name="TConsumer">The consumer type</typeparam>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddTransientMessageConsumer<TMessage, TConsumer>(this IServiceCollection services)
        where TConsumer : class, IMessageConsumer<TMessage>
    {
        services.TryAddTransient<IMessageConsumer<TMessage>, TConsumer>();
        return services;
    }

    /// <summary>
    /// Adds a singleton message consumer to the service collection
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    /// <typeparam name="TConsumer">The consumer type</typeparam>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSingletonMessageConsumer<TMessage, TConsumer>(this IServiceCollection services)
        where TConsumer : class, IMessageConsumer<TMessage>
    {
        services.TryAddSingleton<IMessageConsumer<TMessage>, TConsumer>();
        return services;
    }
}