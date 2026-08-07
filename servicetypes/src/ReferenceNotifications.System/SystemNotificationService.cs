using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Results;
using Fdw.Services.Messaging;
using Fdw.Services.Notifications.Abstractions;
using ReferenceNotifications.System.Logging;
using Fdw.Services.Notifications.Logging;
using Fdw.Services.Notifications.Results;
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
/// Notification service that delivers notifications as in-system messages
/// via the <see cref="IMessageService"/>. Provides lifecycle tracking
/// (delivered, read, dismissed, archived) for each notification.
/// </summary>
public sealed class SystemNotificationService : INotificationService
{
    private readonly SystemNotificationConfiguration _configuration;
    private readonly ILogger<SystemNotificationService> _logger;
    private readonly IMessageService _messageService;
    private readonly string _id;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemNotificationService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="configuration">The system notification configuration.</param>
    /// <param name="messageService">The message service for creating in-system messages.</param>
    public SystemNotificationService(
        ILogger<SystemNotificationService> logger,
        SystemNotificationConfiguration configuration,
        IMessageService messageService)
    {
        _logger = logger ?? NullLogger<SystemNotificationService>.Instance;
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        _id = Guid.NewGuid().ToString("N");
    }

    /// <inheritdoc/>
    public string Id => _id;

    /// <inheritdoc/>
    public string Name => _configuration.Name;

    /// <inheritdoc/>
    public string ServiceType => "System";

    /// <inheritdoc/>
    public bool IsAvailable => true;

    /// <inheritdoc/>
    public INotificationChannel Channel => NotificationChannels.ByName("System");

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
    public async Task<IGenericResult<INotificationResult>> Send(
        INotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        SystemNotificationLogger.SendingNotification(
            _logger,
            _configuration.Name,
            request.Subject);

        var createRequest = new CreateMessageRequest
        {
            TenantId = Guid.Empty,
            MessageType = _configuration.DefaultMessageType,
            Severity = _configuration.DefaultSeverity,
            Subject = request.Subject,
            Body = request.Message,
            ReferenceId = request.RequestId,
        };

        var result = await _messageService.CreateMessage(createRequest, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            SystemNotificationLogger.SendFailed(_logger, _configuration.Name, result.CurrentMessage ?? "Unknown error");

            return GenericResult<INotificationResult>.Failure(
                NotificationResultCodes.ByName("SendFailed"),
                ResultDetails.Create().With("Message", result.CurrentMessage ?? "Unknown error"));
        }

        SystemNotificationLogger.NotificationSent(_logger, _configuration.Name, request.Subject);

        return GenericResult<INotificationResult>.Success(
            NotificationResult.Success(request.RequestId));
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
