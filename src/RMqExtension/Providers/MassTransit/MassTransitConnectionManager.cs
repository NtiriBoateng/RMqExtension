using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RMqExtension.Abstractions;
using RMqExtension.Configuration;

namespace RMqExtension.Providers.MassTransit;

/// <summary>
/// MassTransit connection manager implementation
/// </summary>
public class MassTransitConnectionManager : IConnectionManager
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<MassTransitConnectionManager> _logger;
    private IBusControl? _busControl;
    private bool _disposed = false;

    public bool IsConnected => _busControl != null;

    public event EventHandler? Connected;
    public event EventHandler? Disconnected;
    public event EventHandler? Recovering;
    public event EventHandler? Recovered;

    public MassTransitConnectionManager(IOptions<RabbitMqOptions> options, ILogger<MassTransitConnectionManager> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected) return;

        try
        {
            _busControl = Bus.Factory.CreateUsingRabbitMq(cfg =>
            {
                cfg.Host(new Uri(_options.ConnectionString), h =>
                {
                    // Connection configuration is handled by the URI
                });

                // Configure retry policy
                cfg.UseMessageRetry(r => r.Interval(_options.RetryAttempts, TimeSpan.FromSeconds(5)));
            });

            await _busControl.StartAsync(cancellationToken);

            _logger.LogInformation("Connected to RabbitMQ via MassTransit at {ConnectionString}", _options.ConnectionString);
            Connected?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to RabbitMQ via MassTransit at {ConnectionString}", _options.ConnectionString);
            throw;
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (_busControl != null)
        {
            try
            {
                await _busControl.StopAsync(cancellationToken);
                _logger.LogInformation("Disconnected from RabbitMQ via MassTransit");
                Disconnected?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during disconnect from RabbitMQ via MassTransit");
            }
        }
    }

    internal IBusControl GetBus()
    {
        if (!IsConnected || _busControl == null)
            throw new InvalidOperationException("Not connected to RabbitMQ. Call ConnectAsync first.");
        
        return _busControl;
    }

    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            _busControl?.Stop();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disposing MassTransit bus");
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}