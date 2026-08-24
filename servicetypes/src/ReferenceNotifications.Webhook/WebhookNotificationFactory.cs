using System;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Notifications.Abstractions;
using ReferenceNotifications.Webhook.Logging;
using Microsoft.Extensions.Logging;
using ReferenceNotifications.Webhook;
using Fdw.Services.Notifications;
using Fdw.Services;
using Fdw;

namespace ReferenceNotifications.Webhook;

/// <summary>
/// Factory for creating <see cref="WebhookNotificationService"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// Registered as singleton in DI. Dependencies via constructor, config via CreateNotification().
/// </para>
/// <para>
/// This factory follows the MessageLogging pattern: all operations are logged via
/// <see cref="WebhookNotificationFactoryLogger"/> and messages are returned in results.
/// Exceptions are caught, logged, and returned - never rethrown.
/// </para>
/// </remarks>
public sealed class WebhookNotificationFactory : IWebhookNotificationFactory
{
    private readonly ILogger<WebhookNotificationFactory> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly System.Net.Http.IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookNotificationFactory"/> class.
    /// </summary>
    /// <param name="logger">The logger for factory operations.</param>
    /// <param name="loggerFactory">The logger factory for creating service instance loggers.</param>
    /// <param name="httpClientFactory">The HTTP client factory for webhook calls.</param>
    public WebhookNotificationFactory(
        ILogger<WebhookNotificationFactory> logger,
        ILoggerFactory loggerFactory,
        System.Net.Http.IHttpClientFactory httpClientFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    /// <inheritdoc/>
    public Task<IGenericResult<INotificationService>> CreateNotification(WebhookNotificationConfiguration configuration)
    {
        if (configuration == null)
        {
            return Task.FromResult<IGenericResult<INotificationService>>(
                GenericResult<INotificationService>.Failure(
                    WebhookNotificationFactoryLogger.ConfigurationNull(_logger)));
        }

        if (string.IsNullOrWhiteSpace(configuration.Url))
        {
            return Task.FromResult<IGenericResult<INotificationService>>(
                GenericResult<INotificationService>.Failure(
                    WebhookNotificationFactoryLogger.WebhookUrlMissing(_logger, configuration.Name)));
        }

        try
        {
            WebhookNotificationFactoryLogger.CreatingNotification(_logger, configuration.Name);

            var serviceLogger = _loggerFactory.CreateLogger<WebhookNotificationService>();
            var service = new WebhookNotificationService(serviceLogger, _httpClientFactory, configuration);

            WebhookNotificationFactoryLogger.NotificationCreated(
                _logger,
                configuration.Name,
                configuration.Url);

            return Task.FromResult<IGenericResult<INotificationService>>(
                GenericResult<INotificationService>.Success(service));
        }
        catch (Exception ex)
        {
            return Task.FromResult<IGenericResult<INotificationService>>(
                GenericResult<INotificationService>.Failure(
                    WebhookNotificationFactoryLogger.CreationFailed(_logger, configuration.Name, ex.Message)));
        }
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IGenericNotification>> CreateNotification(IGenericConfiguration configuration)
    {
        if (configuration == null)
        {
            return GenericResult<IGenericNotification>.Failure(
                WebhookNotificationFactoryLogger.ConfigurationNull(_logger));
        }

        if (configuration is not WebhookNotificationConfiguration webhookConfig)
        {
            return GenericResult<IGenericNotification>.Failure(
                WebhookNotificationFactoryLogger.InvalidConfigurationType(
                    _logger,
                    configuration.GetType().Name));
        }

        var result = await CreateNotification(webhookConfig).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return result.ToNewResult<IGenericNotification>();
        }

        return GenericResult<IGenericNotification>.Success(result.Value!);
    }

    /// <inheritdoc/>
    IGenericResult<INotificationService> IServiceFactory<INotificationService, WebhookNotificationConfiguration>.Create(
        WebhookNotificationConfiguration configuration)
    {
        if (configuration == null)
        {
            return GenericResult<INotificationService>.Failure(
                WebhookNotificationFactoryLogger.ConfigurationNull(_logger));
        }

        if (string.IsNullOrWhiteSpace(configuration.Url))
        {
            return GenericResult<INotificationService>.Failure(
                WebhookNotificationFactoryLogger.WebhookUrlMissing(_logger, configuration.Name));
        }

        try
        {
            WebhookNotificationFactoryLogger.CreatingNotification(_logger, configuration.Name);

            var serviceLogger = _loggerFactory.CreateLogger<WebhookNotificationService>();
            var service = new WebhookNotificationService(serviceLogger, _httpClientFactory, configuration);

            WebhookNotificationFactoryLogger.NotificationCreated(
                _logger,
                configuration.Name,
                configuration.Url);

            return GenericResult<INotificationService>.Success(service);
        }
        catch (Exception ex)
        {
            return GenericResult<INotificationService>.Failure(
                WebhookNotificationFactoryLogger.CreationFailed(_logger, configuration.Name, ex.Message));
        }
    }

    /// <inheritdoc/>
    IGenericResult<INotificationService> IServiceFactory<INotificationService>.Create(
        IGenericConfiguration configuration)
    {
        if (configuration == null)
        {
            return GenericResult<INotificationService>.Failure(
                WebhookNotificationFactoryLogger.ConfigurationNull(_logger));
        }

        if (configuration is not WebhookNotificationConfiguration webhookConfig)
        {
            return GenericResult<INotificationService>.Failure(
                WebhookNotificationFactoryLogger.InvalidConfigurationType(
                    _logger,
                    configuration.GetType().Name));
        }

        return ((IServiceFactory<INotificationService, WebhookNotificationConfiguration>)this).Create(webhookConfig);
    }

    /// <inheritdoc/>
    IGenericResult<T> IServiceFactory.Create<T>(IGenericConfiguration configuration)
    {
        if (configuration == null)
        {
            return GenericResult<T>.Failure(
                WebhookNotificationFactoryLogger.ConfigurationNull(_logger));
        }

        if (configuration is not WebhookNotificationConfiguration webhookConfig)
        {
            return GenericResult<T>.Failure(
                WebhookNotificationFactoryLogger.InvalidConfigurationType(
                    _logger,
                    configuration.GetType().Name));
        }

        try
        {
            WebhookNotificationFactoryLogger.CreatingNotification(_logger, webhookConfig.Name);

            var serviceLogger = _loggerFactory.CreateLogger<WebhookNotificationService>();
            var service = new WebhookNotificationService(serviceLogger, _httpClientFactory, webhookConfig);

            WebhookNotificationFactoryLogger.NotificationCreated(
                _logger,
                webhookConfig.Name,
                webhookConfig.Url!);

            if (service is T typedService)
            {
                return GenericResult<T>.Success(typedService);
            }

            return GenericResult<T>.Failure(
                WebhookNotificationFactoryLogger.UnexpectedNotificationType(_logger, typeof(T).Name));
        }
        catch (Exception ex)
        {
            return GenericResult<T>.Failure(
                WebhookNotificationFactoryLogger.CreationFailed(_logger, webhookConfig.Name, ex.Message));
        }
    }

    /// <inheritdoc/>
    IGenericResult<IGenericService> IServiceFactory.Create(IGenericConfiguration configuration)
    {
        if (configuration == null)
        {
            return GenericResult<IGenericService>.Failure(
                WebhookNotificationFactoryLogger.ConfigurationNull(_logger));
        }

        if (configuration is not WebhookNotificationConfiguration webhookConfig)
        {
            return GenericResult<IGenericService>.Failure(
                WebhookNotificationFactoryLogger.InvalidConfigurationType(
                    _logger,
                    configuration.GetType().Name));
        }

        try
        {
            WebhookNotificationFactoryLogger.CreatingNotification(_logger, webhookConfig.Name);

            var serviceLogger = _loggerFactory.CreateLogger<WebhookNotificationService>();
            var service = new WebhookNotificationService(serviceLogger, _httpClientFactory, webhookConfig);

            WebhookNotificationFactoryLogger.NotificationCreated(
                _logger,
                webhookConfig.Name,
                webhookConfig.Url!);

            return GenericResult<IGenericService>.Success(service);
        }
        catch (Exception ex)
        {
            return GenericResult<IGenericService>.Failure(
                WebhookNotificationFactoryLogger.CreationFailed(_logger, webhookConfig.Name, ex.Message));
        }
    }
}
