using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RMqExtension.Abstractions;
using RMqExtension.Configuration;
using System.Collections.Concurrent;

namespace RMqExtension.Providers.Direct;

/// <summary>
/// Direct RabbitMQ connection manager implementation
/// </summary>
public class DirectConnectionManager : IConnectionManager
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<DirectConnectionManager> _logger;
    private IConnection? _connection;
    private IModel? _channel;
    private bool _disposed = false;
    private readonly object _lock = new();

    public bool IsConnected => _connection?.IsOpen == true;

    public event EventHandler? Connected;
    public event EventHandler? Disconnected;
    public event EventHandler? Recovering;
    public event EventHandler? Recovered;

    public DirectConnectionManager(IOptions<RabbitMqOptions> options, ILogger<DirectConnectionManager> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected) return;

        lock (_lock)
        {
            if (IsConnected) return;

            try
            {
                var factory = new ConnectionFactory
                {
                    Uri = new Uri(_options.ConnectionString),
                    AutomaticRecoveryEnabled = _options.AutomaticRecoveryEnabled,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(_options.NetworkRecoveryIntervalSeconds),
                    RequestedConnectionTimeout = TimeSpan.FromSeconds(_options.ConnectionTimeoutSeconds)
                };

                // Add custom connection properties
                foreach (var prop in _options.ConnectionProperties)
                {
                    factory.ClientProperties[prop.Key] = prop.Value;
                }

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                // Setup event handlers
                _connection.ConnectionShutdown += OnConnectionShutdown;
                
                if (_connection is IRecoverable recoverable)
                {
                    recoverable.Recovery += OnRecovery;
                }

                _logger.LogInformation("Connected to RabbitMQ at {ConnectionString}", _options.ConnectionString);
                Connected?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to RabbitMQ at {ConnectionString}", _options.ConnectionString);
                throw;
            }
        }

        await Task.CompletedTask;
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            try
            {
                _channel?.Close();
                _connection?.Close();
                _logger.LogInformation("Disconnected from RabbitMQ");
                Disconnected?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during disconnect from RabbitMQ");
            }
        }

        await Task.CompletedTask;
    }

    private void OnConnectionShutdown(object? sender, ShutdownEventArgs e)
    {
        _logger.LogWarning("RabbitMQ connection shutdown: {Reason}", e.ReplyText);
        Disconnected?.Invoke(this, EventArgs.Empty);
        
        if (!e.Initiator.Equals(ShutdownInitiator.Application))
        {
            Recovering?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnRecovery(object? sender, EventArgs e)
    {
        _logger.LogInformation("RabbitMQ connection recovered successfully");
        Recovered?.Invoke(this, EventArgs.Empty);
    }

    internal IModel GetChannel()
    {
        if (!IsConnected || _channel == null)
            throw new InvalidOperationException("Not connected to RabbitMQ. Call ConnectAsync first.");
        
        return _channel;
    }

    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disposing RabbitMQ connection");
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}