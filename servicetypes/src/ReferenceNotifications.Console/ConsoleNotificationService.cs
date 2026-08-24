using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Results;
using Fdw.Services.Notifications.Abstractions;
using ReferenceNotifications.Console.Logging;
using Fdw.Services.Notifications.Logging;
using Fdw.Services.Notifications.Results;
using Microsoft.Extensions.Logging;
using ReferenceNotifications.Console;
using Fdw.Services.Notifications;
using Fdw.Services;
using Fdw;

namespace ReferenceNotifications.Console;

/// <summary>
/// Notification service that logs notification content at Information level.
/// Intended for development and test environments where sending real notifications
/// is undesirable. All notification content is emitted via structured logging.
/// </summary>
public sealed class ConsoleNotificationService : INotificationService
{
    private readonly ConsoleNotificationConfiguration _configuration;
    private readonly ILogger<ConsoleNotificationService> _logger;
    private readonly string _id;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleNotificationService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="configuration">The console notification configuration.</param>
    public ConsoleNotificationService(
        ILogger<ConsoleNotificationService> logger,
        ConsoleNotificationConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _id = Guid.NewGuid().ToString("N");
    }

    /// <inheritdoc/>
    public string Id => _id;

    /// <inheritdoc/>
    public string Name => _configuration.Name;

    /// <inheritdoc/>
    public string ServiceType => "Console";

    /// <inheritdoc/>
    public bool IsAvailable => true;

    /// <inheritdoc/>
    public INotificationChannel Channel => NotificationChannels.ByName("Console");

    /// <inheritdoc/>
    public IGenericResult Validate(INotificationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            NotificationLogger.EmptyMessage(_logger);
            return GenericResult.Failure(NotificationResultCodes.ByName("EmptyMessage"));
        }

        return GenericResult.Success();
    }

    /// <inheritdoc/>
    public Task<IGenericResult<INotificationResult>> Send(
        INotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        ConsoleNotificationLogger.NotificationLogged(
            _logger,
            _configuration.Name,
            request.Subject,
            request.Message);

        var result = GenericResult<INotificationResult>.Success(
            NotificationResult.Success(request.RequestId));

        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<T>> Execute<T>(IGenericCommand command, CancellationToken cancellationToken = default)
    {
        if (command is not INotificationRequest request)
        {
            return GenericResult<T>.Failure(
                NotificationResultCodes.ByName("UnsupportedCommand"),
                ResultDetails.Create().With("CommandType", command.GetType().Name));
        }

        var result = await Send(request, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return GenericResult<T>.Failure(
                NotificationResultCodes.ByName("SendFailed"),
                ResultDetails.Create().With("Message", result.CurrentMessage ?? "Unknown error"));
        }

        if (result.Value is T typedResult)
        {
            return GenericResult<T>.Success(typedResult);
        }

        return GenericResult<T>.Failure(
            NotificationResultCodes.ByName("UnexpectedResultType"),
            ResultDetails.Create().With("ExpectedType", typeof(T).Name));
    }

    /// <inheritdoc/>
    public async Task<IGenericResult> Execute(IGenericCommand command, CancellationToken cancellationToken = default)
    {
        if (command is not INotificationRequest request)
        {
            return GenericResult.Failure(
                NotificationResultCodes.ByName("UnsupportedCommand"),
                ResultDetails.Create().With("CommandType", command.GetType().Name));
        }

        var result = await Send(request, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess
            ? GenericResult.Success()
            : GenericResult.Failure(
                NotificationResultCodes.ByName("SendFailed"),
                ResultDetails.Create().With("Message", result.CurrentMessage ?? "Unknown error"));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // No unmanaged resources to dispose
    }
}
