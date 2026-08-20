using System;
using System.Net.Http;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Notifications.Abstractions;
using Microsoft.Extensions.Logging;
using ReferenceNotifications.Teams.Logging;

namespace ReferenceNotifications.Teams;

/// <summary>Creates a <see cref="TeamsNotificationService"/> per configured Teams target.</summary>
public sealed class TeamsNotificationFactory : ITeamsNotificationFactory
{
    private readonly ILogger<TeamsNotificationFactory> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>Initializes a new instance of the <see cref="TeamsNotificationFactory"/> class.</summary>
    public TeamsNotificationFactory(
        ILogger<TeamsNotificationFactory> logger,
        ILoggerFactory loggerFactory,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    /// <inheritdoc/>
    public Task<IGenericResult<INotificationService>> CreateNotification(TeamsNotificationConfiguration configuration)
        => Task.FromResult(Build(configuration));

    /// <inheritdoc/>
    public async Task<IGenericResult<IGenericNotification>> CreateNotification(IGenericConfiguration configuration)
    {
        if (Narrow(configuration) is not { } teamsConfig)
        {
            return GenericResult<IGenericNotification>.Failure(Rejected(configuration));
        }

        var result = await CreateNotification(teamsConfig).ConfigureAwait(false);
        return result.IsSuccess
            ? GenericResult<IGenericNotification>.Success(result.Value!)
            : result.ToNewResult<IGenericNotification>();
    }

    /// <inheritdoc/>
    IGenericResult<INotificationService> IServiceFactory<INotificationService, TeamsNotificationConfiguration>.Create(
        TeamsNotificationConfiguration configuration)
        => Build(configuration);

    /// <inheritdoc/>
    IGenericResult<INotificationService> IServiceFactory<INotificationService>.Create(IGenericConfiguration configuration)
        => Narrow(configuration) is { } teamsConfig
            ? Build(teamsConfig)
            : GenericResult<INotificationService>.Failure(Rejected(configuration));

    /// <inheritdoc/>
    IGenericResult<IGenericService> IServiceFactory.Create(IGenericConfiguration configuration)
    {
        if (Narrow(configuration) is not { } teamsConfig)
        {
            return GenericResult<IGenericService>.Failure(Rejected(configuration));
        }

        var built = Build(teamsConfig);
        return built.IsSuccess
            ? GenericResult<IGenericService>.Success(built.Value!)
            : built.ToNewResult<IGenericService>();
    }

    /// <inheritdoc/>
    IGenericResult<T> IServiceFactory.Create<T>(IGenericConfiguration configuration)
    {
        if (Narrow(configuration) is not { } teamsConfig)
        {
            return GenericResult<T>.Failure(Rejected(configuration));
        }

        var built = Build(teamsConfig);
        if (!built.IsSuccess)
        {
            return built.ToNewResult<T>();
        }

        return built.Value is T typedService
            ? GenericResult<T>.Success(typedService)
            : GenericResult<T>.Failure(
                TeamsNotificationFactoryLogger.UnexpectedNotificationType(_logger, typeof(T).Name));
    }

    // Why one Build rather than the construction repeated per interface member: the four entry
    // points differ only in what they accept and return, and a target that is valid through one
    // of them is valid through all of them.
    private IGenericResult<INotificationService> Build(TeamsNotificationConfiguration configuration)
    {
        if (configuration == null)
        {
            return GenericResult<INotificationService>.Failure(
                TeamsNotificationFactoryLogger.ConfigurationNull(_logger));
        }

        // Why this is checked here and not left to the send: a target with no webhook can never
        // deliver, and finding that out at construction names the configuration that is wrong
        // rather than a notification that silently went nowhere.
        if (string.IsNullOrWhiteSpace(configuration.DefaultWebhookUrl))
        {
            return GenericResult<INotificationService>.Failure(
                TeamsNotificationFactoryLogger.WebhookUrlMissing(_logger, configuration.Name));
        }

        try
        {
            TeamsNotificationFactoryLogger.CreatingNotification(_logger, configuration.Name);

            var service = new TeamsNotificationService(
                _loggerFactory.CreateLogger<TeamsNotificationService>(),
                _httpClientFactory,
                configuration);

            TeamsNotificationFactoryLogger.NotificationCreated(_logger, configuration.Name);
            return GenericResult<INotificationService>.Success(service);
        }
        catch (Exception ex)
        {
            return GenericResult<INotificationService>.Failure(
                TeamsNotificationFactoryLogger.CreateFailed(_logger, ex, configuration.Name, ex.Message));
        }
    }

    private static TeamsNotificationConfiguration? Narrow(IGenericConfiguration configuration)
        => configuration as TeamsNotificationConfiguration;

    private IGenericMessage Rejected(IGenericConfiguration configuration)
        => configuration == null
            ? TeamsNotificationFactoryLogger.ConfigurationNull(_logger)
            : TeamsNotificationFactoryLogger.InvalidConfigurationType(_logger, configuration.GetType().Name);
}
