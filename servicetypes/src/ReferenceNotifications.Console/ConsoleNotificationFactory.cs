using System;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Notifications.Abstractions;
using ReferenceNotifications.Console.Logging;
using Microsoft.Extensions.Logging;
using ReferenceNotifications.Console;
using Fdw.Services.Notifications.Console.Commands;
using Fdw.Services.Notifications.Console;
using Fdw.Services.Notifications;
using Fdw.Services;
using Fdw;

namespace ReferenceNotifications.Console;

/// <summary>
/// Factory for creating <see cref="ConsoleNotificationService"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// Registered as singleton in DI. Dependencies via constructor, config via CreateNotification().
/// </para>
/// <para>
/// This factory follows the MessageLogging pattern: all operations are logged via
/// <see cref="ConsoleNotificationLogger"/> and messages are returned in results.
/// Exceptions are caught, logged, and returned - never rethrown.
/// </para>
/// </remarks>
public sealed class ConsoleNotificationFactory : IConsoleNotificationFactory
{
    private readonly ILogger<ConsoleNotificationFactory> _logger;
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleNotificationFactory"/> class.
    /// </summary>
    /// <param name="logger">The logger for factory operations.</param>
    /// <param name="loggerFactory">The logger factory for creating service instance loggers.</param>
    public ConsoleNotificationFactory(
        ILogger<ConsoleNotificationFactory> logger,
        ILoggerFactory loggerFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <inheritdoc/>
    public Task<IGenericResult<INotificationService>> CreateNotification(ConsoleNotificationConfiguration configuration)
    {
        if (configuration == null)
        {
            return Task.FromResult<IGenericResult<INotificationService>>(
                GenericResult<INotificationService>.Failure(
                    ConsoleNotificationLogger.ConfigurationNull(_logger)));
        }

        try
        {
            ConsoleNotificationLogger.CreatingNotification(_logger, configuration.Name);

            var serviceLogger = _loggerFactory.CreateLogger<ConsoleNotificationService>();
            var service = new ConsoleNotificationService(serviceLogger, configuration);

            ConsoleNotificationLogger.NotificationCreated(_logger, configuration.Name);

            return Task.FromResult<IGenericResult<INotificationService>>(
                GenericResult<INotificationService>.Success(service));
        }
        catch (Exception ex)
        {
            return Task.FromResult<IGenericResult<INotificationService>>(
                GenericResult<INotificationService>.Failure(
                    ConsoleNotificationLogger.CreationFailed(_logger, configuration.Name, ex.Message)));
        }
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IGenericNotification>> CreateNotification(IGenericConfiguration configuration)
    {
        if (configuration == null)
        {
            return GenericResult<IGenericNotification>.Failure(
                ConsoleNotificationLogger.ConfigurationNull(_logger));
        }

        if (configuration is not ConsoleNotificationConfiguration consoleConfig)
        {
            return GenericResult<IGenericNotification>.Failure(
                ConsoleNotificationLogger.InvalidConfigurationType(
                    _logger,
                    configuration.GetType().Name));
        }

        var result = await CreateNotification(consoleConfig).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return result.ToNewResult<IGenericNotification>();
        }

        return GenericResult<IGenericNotification>.Success(result.Value!);
    }

    /// <inheritdoc/>
    IGenericResult<INotificationService> IServiceFactory<INotificationService, ConsoleNotificationConfiguration>.Create(
        ConsoleNotificationConfiguration configuration)
    {
        if (configuration == null)
        {
            return GenericResult<INotificationService>.Failure(
                ConsoleNotificationLogger.ConfigurationNull(_logger));
        }

        try
        {
            ConsoleNotificationLogger.CreatingNotification(_logger, configuration.Name);

            var serviceLogger = _loggerFactory.CreateLogger<ConsoleNotificationService>();
            var service = new ConsoleNotificationService(serviceLogger, configuration);

            ConsoleNotificationLogger.NotificationCreated(_logger, configuration.Name);

            return GenericResult<INotificationService>.Success(service);
        }
        catch (Exception ex)
        {
            return GenericResult<INotificationService>.Failure(
                ConsoleNotificationLogger.CreationFailed(_logger, configuration.Name, ex.Message));
        }
    }

    /// <inheritdoc/>
    IGenericResult<INotificationService> IServiceFactory<INotificationService>.Create(
        IGenericConfiguration configuration)
    {
        if (configuration == null)
        {
            return GenericResult<INotificationService>.Failure(
                ConsoleNotificationLogger.ConfigurationNull(_logger));
        }

        if (configuration is not ConsoleNotificationConfiguration consoleConfig)
        {
            return GenericResult<INotificationService>.Failure(
                ConsoleNotificationLogger.InvalidConfigurationType(
                    _logger,
                    configuration.GetType().Name));
        }

        return ((IServiceFactory<INotificationService, ConsoleNotificationConfiguration>)this).Create(consoleConfig);
    }

    /// <inheritdoc/>
    IGenericResult<T> IServiceFactory.Create<T>(IGenericConfiguration configuration)
    {
        if (configuration == null)
        {
            return GenericResult<T>.Failure(
                ConsoleNotificationLogger.ConfigurationNull(_logger));
        }

        if (configuration is not ConsoleNotificationConfiguration consoleConfig)
        {
            return GenericResult<T>.Failure(
                ConsoleNotificationLogger.InvalidConfigurationType(
                    _logger,
                    configuration.GetType().Name));
        }

        try
        {
            ConsoleNotificationLogger.CreatingNotification(_logger, consoleConfig.Name);

            var serviceLogger = _loggerFactory.CreateLogger<ConsoleNotificationService>();
            var service = new ConsoleNotificationService(serviceLogger, consoleConfig);

            ConsoleNotificationLogger.NotificationCreated(_logger, consoleConfig.Name);

            if (service is T typedService)
            {
                return GenericResult<T>.Success(typedService);
            }

            return GenericResult<T>.Failure(
                ConsoleNotificationLogger.UnexpectedNotificationType(_logger, typeof(T).Name));
        }
        catch (Exception ex)
        {
            return GenericResult<T>.Failure(
                ConsoleNotificationLogger.CreationFailed(_logger, consoleConfig.Name, ex.Message));
        }
    }

    /// <inheritdoc/>
    IGenericResult<IGenericService> IServiceFactory.Create(IGenericConfiguration configuration)
    {
        if (configuration == null)
        {
            return GenericResult<IGenericService>.Failure(
                ConsoleNotificationLogger.ConfigurationNull(_logger));
        }

        if (configuration is not ConsoleNotificationConfiguration consoleConfig)
        {
            return GenericResult<IGenericService>.Failure(
                ConsoleNotificationLogger.InvalidConfigurationType(
                    _logger,
                    configuration.GetType().Name));
        }

        try
        {
            ConsoleNotificationLogger.CreatingNotification(_logger, consoleConfig.Name);

            var serviceLogger = _loggerFactory.CreateLogger<ConsoleNotificationService>();
            var service = new ConsoleNotificationService(serviceLogger, consoleConfig);

            ConsoleNotificationLogger.NotificationCreated(_logger, consoleConfig.Name);

            return GenericResult<IGenericService>.Success(service);
        }
        catch (Exception ex)
        {
            return GenericResult<IGenericService>.Failure(
                ConsoleNotificationLogger.CreationFailed(_logger, consoleConfig.Name, ex.Message));
        }
    }
}
