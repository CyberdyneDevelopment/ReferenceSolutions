using System;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Messaging;
using Fdw.Services.Notifications.Abstractions;
using ReferenceNotifications.System.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReferenceNotifications.System;
using Fdw.Services.Notifications.System.Commands;
using Fdw.Services.Notifications.System;
using Fdw.Services.Notifications;
using Fdw.Services;
using Fdw;

namespace ReferenceNotifications.System;

/// <summary>
/// Factory for creating <see cref="SystemNotificationService"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// Registered as singleton in DI. Dependencies via constructor, config via CreateNotification().
/// </para>
/// <para>
/// This factory follows the MessageLogging pattern: all operations are logged via
/// <see cref="SystemNotificationLogger"/> and messages are returned in results.
/// Exceptions are caught, logged, and returned - never rethrown.
/// </para>
/// </remarks>
public sealed class SystemNotificationFactory : ISystemNotificationFactory
{
    private readonly ILogger<SystemNotificationFactory> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IMessageService _messageService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemNotificationFactory"/> class.
    /// </summary>
    /// <param name="logger">The logger for factory operations.</param>
    /// <param name="loggerFactory">The logger factory for creating service instance loggers.</param>
    /// <param name="messageService">The message service for creating in-system messages.</param>
    public SystemNotificationFactory(
        ILogger<SystemNotificationFactory> logger,
        ILoggerFactory loggerFactory,
        IMessageService messageService)
    {
        _logger = logger ?? NullLogger<SystemNotificationFactory>.Instance;
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
    }

    /// <inheritdoc/>
    public Task<IGenericResult<INotificationService>> CreateNotification(SystemNotificationConfiguration configuration)
    {
        if (configuration == null)
        {
            return Task.FromResult<IGenericResult<INotificationService>>(
                GenericResult<INotificationService>.Failure(
                    SystemNotificationLogger.ConfigurationNull(_logger)));
        }

        try
        {
            SystemNotificationLogger.CreatingNotification(_logger, configuration.Name);

            var serviceLogger = _loggerFactory.CreateLogger<SystemNotificationService>();
            var service = new SystemNotificationService(serviceLogger, configuration, _messageService);

            SystemNotificationLogger.NotificationCreated(_logger, configuration.Name);

            return Task.FromResult<IGenericResult<INotificationService>>(
                GenericResult<INotificationService>.Success(service));
        }
        catch (Exception ex)
        {
            return Task.FromResult<IGenericResult<INotificationService>>(
                GenericResult<INotificationService>.Failure(
                    SystemNotificationLogger.CreationFailed(_logger, configuration.Name, ex.Message)));
        }
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IGenericNotification>> CreateNotification(IGenericConfiguration configuration)
    {
        if (configuration == null)
        {
            return GenericResult<IGenericNotification>.Failure(
                SystemNotificationLogger.ConfigurationNull(_logger));
        }

        if (configuration is not SystemNotificationConfiguration systemConfig)
        {
            return GenericResult<IGenericNotification>.Failure(
                SystemNotificationLogger.InvalidConfigurationType(
                    _logger,
                    configuration.GetType().Name));
        }

        var result = await CreateNotification(systemConfig).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return result.ToNewResult<IGenericNotification>();
        }

        return GenericResult<IGenericNotification>.Success(result.Value!);
    }

    /// <inheritdoc/>
    IGenericResult<INotificationService> IServiceFactory<INotificationService, SystemNotificationConfiguration>.Create(
        SystemNotificationConfiguration configuration)
    {
        if (configuration == null)
        {
            return GenericResult<INotificationService>.Failure(
                SystemNotificationLogger.ConfigurationNull(_logger));
        }

        try
        {
            SystemNotificationLogger.CreatingNotification(_logger, configuration.Name);

            var serviceLogger = _loggerFactory.CreateLogger<SystemNotificationService>();
            var service = new SystemNotificationService(serviceLogger, configuration, _messageService);

            SystemNotificationLogger.NotificationCreated(_logger, configuration.Name);

            return GenericResult<INotificationService>.Success(service);
        }
        catch (Exception ex)
        {
            return GenericResult<INotificationService>.Failure(
                SystemNotificationLogger.CreationFailed(_logger, configuration.Name, ex.Message));
        }
    }

    /// <inheritdoc/>
    IGenericResult<INotificationService> IServiceFactory<INotificationService>.Create(
        IGenericConfiguration configuration)
    {
        if (configuration == null)
        {
            return GenericResult<INotificationService>.Failure(
                SystemNotificationLogger.ConfigurationNull(_logger));
        }

        if (configuration is not SystemNotificationConfiguration systemConfig)
        {
            return GenericResult<INotificationService>.Failure(
                SystemNotificationLogger.InvalidConfigurationType(
                    _logger,
                    configuration.GetType().Name));
        }

        return ((IServiceFactory<INotificationService, SystemNotificationConfiguration>)this).Create(systemConfig);
    }

    /// <inheritdoc/>
    IGenericResult<T> IServiceFactory.Create<T>(IGenericConfiguration configuration)
    {
        if (configuration == null)
        {
            return GenericResult<T>.Failure(
                SystemNotificationLogger.ConfigurationNull(_logger));
        }

        if (configuration is not SystemNotificationConfiguration systemConfig)
        {
            return GenericResult<T>.Failure(
                SystemNotificationLogger.InvalidConfigurationType(
                    _logger,
                    configuration.GetType().Name));
        }

        try
        {
            SystemNotificationLogger.CreatingNotification(_logger, systemConfig.Name);

            var serviceLogger = _loggerFactory.CreateLogger<SystemNotificationService>();
            var service = new SystemNotificationService(serviceLogger, systemConfig, _messageService);

            SystemNotificationLogger.NotificationCreated(_logger, systemConfig.Name);

            if (service is T typedService)
            {
                return GenericResult<T>.Success(typedService);
            }

            return GenericResult<T>.Failure(
                SystemNotificationLogger.UnexpectedNotificationType(_logger, typeof(T).Name));
        }
        catch (Exception ex)
        {
            return GenericResult<T>.Failure(
                SystemNotificationLogger.CreationFailed(_logger, systemConfig.Name, ex.Message));
        }
    }

    /// <inheritdoc/>
    IGenericResult<IGenericService> IServiceFactory.Create(IGenericConfiguration configuration)
    {
        if (configuration == null)
        {
            return GenericResult<IGenericService>.Failure(
                SystemNotificationLogger.ConfigurationNull(_logger));
        }

        if (configuration is not SystemNotificationConfiguration systemConfig)
        {
            return GenericResult<IGenericService>.Failure(
                SystemNotificationLogger.InvalidConfigurationType(
                    _logger,
                    configuration.GetType().Name));
        }

        try
        {
            SystemNotificationLogger.CreatingNotification(_logger, systemConfig.Name);

            var serviceLogger = _loggerFactory.CreateLogger<SystemNotificationService>();
            var service = new SystemNotificationService(serviceLogger, systemConfig, _messageService);

            SystemNotificationLogger.NotificationCreated(_logger, systemConfig.Name);

            return GenericResult<IGenericService>.Success(service);
        }
        catch (Exception ex)
        {
            return GenericResult<IGenericService>.Failure(
                SystemNotificationLogger.CreationFailed(_logger, systemConfig.Name, ex.Message));
        }
    }
}
