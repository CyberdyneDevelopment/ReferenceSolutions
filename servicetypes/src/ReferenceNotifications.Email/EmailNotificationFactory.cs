using System;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Notifications.Abstractions;
using ReferenceNotifications.Email.Logging;
using Microsoft.Extensions.Logging;
using ReferenceNotifications.Email;
using Fdw.Services.Notifications;
using Fdw.Services;
using Fdw;

namespace ReferenceNotifications.Email;

/// <summary>
/// Factory for creating <see cref="EmailNotificationService"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// Registered as singleton in DI. Dependencies via constructor, config via CreateNotification().
/// </para>
/// <para>
/// This factory follows the MessageLogging pattern: all operations are logged via
/// <see cref="EmailNotificationFactoryLogger"/> and messages are returned in results.
/// Exceptions are caught, logged, and returned - never rethrown.
/// </para>
/// </remarks>
public sealed class EmailNotificationFactory : IEmailNotificationFactory
{
    private readonly ILogger<EmailNotificationFactory> _logger;
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmailNotificationFactory"/> class.
    /// </summary>
    /// <param name="logger">The logger for factory operations.</param>
    /// <param name="loggerFactory">The logger factory for creating service instance loggers.</param>
    public EmailNotificationFactory(
        ILogger<EmailNotificationFactory> logger,
        ILoggerFactory loggerFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <inheritdoc/>
    public Task<IGenericResult<INotificationService>> CreateNotification(EmailNotificationConfiguration configuration)
    {
        if (configuration == null)
        {
            return Task.FromResult<IGenericResult<INotificationService>>(
                GenericResult<INotificationService>.Failure(
                    EmailNotificationFactoryLogger.ConfigurationNull(_logger)));
        }

        try
        {
            EmailNotificationFactoryLogger.CreatingNotification(_logger, configuration.Name);

            // Get a scoped logger for the notification service instance
            var serviceLogger = _loggerFactory.CreateLogger<EmailNotificationService>();

            var service = new EmailNotificationService(serviceLogger, configuration);

            EmailNotificationFactoryLogger.NotificationCreated(
                _logger,
                configuration.Name,
                configuration.SmtpHost);

            return Task.FromResult<IGenericResult<INotificationService>>(
                GenericResult<INotificationService>.Success(service));
        }
        catch (Exception ex)
        {
            return Task.FromResult<IGenericResult<INotificationService>>(
                GenericResult<INotificationService>.Failure(
                    EmailNotificationFactoryLogger.CreationFailed(_logger, configuration.Name, ex.Message)));
        }
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IGenericNotification>> CreateNotification(IGenericConfiguration configuration)
    {
        if (configuration == null)
        {
            return GenericResult<IGenericNotification>.Failure(
                EmailNotificationFactoryLogger.ConfigurationNull(_logger));
        }

        if (configuration is not EmailNotificationConfiguration emailConfig)
        {
            return GenericResult<IGenericNotification>.Failure(
                EmailNotificationFactoryLogger.InvalidConfigurationType(
                    _logger,
                    configuration.GetType().Name));
        }

        var result = await CreateNotification(emailConfig).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return result.ToNewResult<IGenericNotification>();
        }

        return GenericResult<IGenericNotification>.Success(result.Value!);
    }

    /// <inheritdoc/>
    IGenericResult<INotificationService> IServiceFactory<INotificationService, EmailNotificationConfiguration>.Create(
        EmailNotificationConfiguration configuration)
    {
        if (configuration == null)
        {
            return GenericResult<INotificationService>.Failure(
                EmailNotificationFactoryLogger.ConfigurationNull(_logger));
        }

        try
        {
            EmailNotificationFactoryLogger.CreatingNotification(_logger, configuration.Name);

            var serviceLogger = _loggerFactory.CreateLogger<EmailNotificationService>();
            var service = new EmailNotificationService(serviceLogger, configuration);

            EmailNotificationFactoryLogger.NotificationCreated(
                _logger,
                configuration.Name,
                configuration.SmtpHost);

            return GenericResult<INotificationService>.Success(service);
        }
        catch (Exception ex)
        {
            return GenericResult<INotificationService>.Failure(
                EmailNotificationFactoryLogger.CreationFailed(_logger, configuration.Name, ex.Message));
        }
    }

    /// <inheritdoc/>
    IGenericResult<INotificationService> IServiceFactory<INotificationService>.Create(
        IGenericConfiguration configuration)
    {
        if (configuration == null)
        {
            return GenericResult<INotificationService>.Failure(
                EmailNotificationFactoryLogger.ConfigurationNull(_logger));
        }

        if (configuration is not EmailNotificationConfiguration emailConfig)
        {
            return GenericResult<INotificationService>.Failure(
                EmailNotificationFactoryLogger.InvalidConfigurationType(
                    _logger,
                    configuration.GetType().Name));
        }

        try
        {
            EmailNotificationFactoryLogger.CreatingNotification(_logger, emailConfig.Name);

            var serviceLogger = _loggerFactory.CreateLogger<EmailNotificationService>();
            var service = new EmailNotificationService(serviceLogger, emailConfig);

            EmailNotificationFactoryLogger.NotificationCreated(
                _logger,
                emailConfig.Name,
                emailConfig.SmtpHost);

            return GenericResult<INotificationService>.Success(service);
        }
        catch (Exception ex)
        {
            return GenericResult<INotificationService>.Failure(
                EmailNotificationFactoryLogger.CreationFailed(_logger, emailConfig.Name, ex.Message));
        }
    }

    /// <inheritdoc/>
    IGenericResult<T> IServiceFactory.Create<T>(IGenericConfiguration configuration)
    {
        if (configuration == null)
        {
            return GenericResult<T>.Failure(
                EmailNotificationFactoryLogger.ConfigurationNull(_logger));
        }

        if (configuration is not EmailNotificationConfiguration emailConfig)
        {
            return GenericResult<T>.Failure(
                EmailNotificationFactoryLogger.InvalidConfigurationType(
                    _logger,
                    configuration.GetType().Name));
        }

        try
        {
            EmailNotificationFactoryLogger.CreatingNotification(_logger, emailConfig.Name);

            var serviceLogger = _loggerFactory.CreateLogger<EmailNotificationService>();
            var service = new EmailNotificationService(serviceLogger, emailConfig);

            EmailNotificationFactoryLogger.NotificationCreated(
                _logger,
                emailConfig.Name,
                emailConfig.SmtpHost);

            if (service is T typedService)
            {
                return GenericResult<T>.Success(typedService);
            }

            return GenericResult<T>.Failure(
                EmailNotificationFactoryLogger.UnexpectedNotificationType(_logger, typeof(T).Name));
        }
        catch (Exception ex)
        {
            return GenericResult<T>.Failure(
                EmailNotificationFactoryLogger.CreationFailed(_logger, emailConfig.Name, ex.Message));
        }
    }

    /// <inheritdoc/>
    IGenericResult<IGenericService> IServiceFactory.Create(IGenericConfiguration configuration)
    {
        if (configuration == null)
        {
            return GenericResult<IGenericService>.Failure(
                EmailNotificationFactoryLogger.ConfigurationNull(_logger));
        }

        if (configuration is not EmailNotificationConfiguration emailConfig)
        {
            return GenericResult<IGenericService>.Failure(
                EmailNotificationFactoryLogger.InvalidConfigurationType(
                    _logger,
                    configuration.GetType().Name));
        }

        try
        {
            EmailNotificationFactoryLogger.CreatingNotification(_logger, emailConfig.Name);

            var serviceLogger = _loggerFactory.CreateLogger<EmailNotificationService>();
            var service = new EmailNotificationService(serviceLogger, emailConfig);

            EmailNotificationFactoryLogger.NotificationCreated(
                _logger,
                emailConfig.Name,
                emailConfig.SmtpHost);

            return GenericResult<IGenericService>.Success(service);
        }
        catch (Exception ex)
        {
            return GenericResult<IGenericService>.Failure(
                EmailNotificationFactoryLogger.CreationFailed(_logger, emailConfig.Name, ex.Message));
        }
    }
}
