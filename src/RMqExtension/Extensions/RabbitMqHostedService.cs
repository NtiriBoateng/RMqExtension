using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RMqExtension.Abstractions;

namespace RMqExtension.Extensions;

/// <summary>
/// Hosted service for managing RabbitMQ connection lifecycle
/// </summary>
public class RabbitMqHostedService : IHostedService
{
    private readonly IConnectionManager _connectionManager;
    private readonly ILogger<RabbitMqHostedService> _logger;

    public RabbitMqHostedService(
        IConnectionManager connectionManager,
        ILogger<RabbitMqHostedService> logger)
    {
        _connectionManager = connectionManager;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _connectionManager.ConnectAsync(cancellationToken);
            _logger.LogInformation("RabbitMQ connection started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start RabbitMQ connection");
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _connectionManager.DisconnectAsync(cancellationToken);
            _connectionManager.Dispose();
            _logger.LogInformation("RabbitMQ connection stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping RabbitMQ connection");
        }
    }
}