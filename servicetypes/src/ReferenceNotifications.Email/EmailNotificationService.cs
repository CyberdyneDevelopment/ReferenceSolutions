using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Results;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Notifications.Extensions;
using Fdw.Services.Notifications.Logging;
using Fdw.Services.Notifications.Results;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using ReferenceNotifications.Email;
using Fdw.Services.Notifications;
using Fdw.Services;
using Fdw;

namespace ReferenceNotifications.Email;

/// <summary>
/// Notification service for sending emails via SMTP using MailKit.
/// </summary>
public sealed class EmailNotificationService : INotificationService
{
    private readonly EmailNotificationConfiguration _configuration;
    private readonly ILogger<EmailNotificationService> _logger;
    private readonly string _id;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmailNotificationService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="configuration">The email configuration.</param>
    public EmailNotificationService(
        ILogger<EmailNotificationService> logger,
        EmailNotificationConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _id = Guid.NewGuid().ToString("N");
    }

    /// <summary>
    /// Gets the current email configuration.
    /// </summary>
    private EmailNotificationConfiguration Configuration => _configuration;

    /// <inheritdoc/>
    public string Id => _id;

    /// <inheritdoc/>
    public string Name => _configuration.Name;

    /// <inheritdoc/>
    public string ServiceType => "Email";

    /// <inheritdoc/>
    public bool IsAvailable => true;

    /// <inheritdoc/>
    public INotificationChannel Channel => NotificationChannels.ByName("Email");

    /// <inheritdoc/>
    public IGenericResult Validate(INotificationRequest request)
    {
        if (request.Recipients.Count == 0)
        {
            NotificationLogger.NoRecipients(_logger);
            return GenericResult.Failure(NotificationResultCodes.ByName("NoRecipients"));
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            NotificationLogger.EmptyMessage(_logger);
            return GenericResult.Failure(NotificationResultCodes.ByName("EmptyMessage"));
        }

        // Validate email addresses
        foreach (var recipient in request.Recipients)
        {
            if (!IsValidEmail(recipient))
            {
                NotificationLogger.ValidationFailed(_logger, $"Invalid email address: {recipient}");
                return GenericResult.Failure(
                    NotificationResultCodes.ByName("InvalidEmailAddress"),
                    ResultDetails.Create().With("EmailAddress", recipient));
            }
        }

        return GenericResult.Success();
    }

    /// <inheritdoc/>
    // MA0051: Method length acceptable - sequential SMTP email flow (build message, connect, authenticate, send, disconnect)
#pragma warning disable MA0051 // Method is too long
    public async Task<IGenericResult<INotificationResult>> Send(
        INotificationRequest request,
        CancellationToken cancellationToken = default)
#pragma warning restore MA0051
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(Configuration.FromName ?? Configuration.FromAddress, Configuration.FromAddress));

            foreach (var recipient in request.Recipients)
            {
                message.To.Add(MailboxAddress.Parse(recipient));
            }

            if (!string.IsNullOrEmpty(Configuration.ReplyToAddress))
            {
                message.ReplyTo.Add(MailboxAddress.Parse(Configuration.ReplyToAddress));
            }

            message.Subject = request.Subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = request.Message,
                TextBody = request.Message.StripHtmlTags()
            };

            message.Body = bodyBuilder.ToMessageBody();

            // Set priority header
            if (string.Equals(request.Priority.Name, "High", StringComparison.Ordinal) || string.Equals(request.Priority.Name, "Critical", StringComparison.Ordinal))
            {
                message.Importance = MessageImportance.High;
            }

            NotificationLogger.ConnectingToSmtp(_logger, Configuration.SmtpHost, Configuration.SmtpPort);

            using var client = new SmtpClient();

            var secureSocketOptions = Configuration.UseSsl
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.None;

            await client.ConnectAsync(
                Configuration.SmtpHost,
                Configuration.SmtpPort,
                secureSocketOptions,
                cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(Configuration.Username)
                && !string.IsNullOrEmpty(Configuration.Password))
            {
                NotificationLogger.SmtpAuthenticating(_logger);
                await client.AuthenticateAsync(
                    Configuration.Username,
                    Configuration.Password,
                    cancellationToken).ConfigureAwait(false);
            }

            var response = await client.SendAsync(message, cancellationToken).ConfigureAwait(false);
            await client.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);

            NotificationLogger.EmailSent(_logger, request.Recipients.Count);

            return GenericResult<INotificationResult>.Success(
                NotificationResult.Success(request.RequestId, response));
        }
        catch (Exception ex)
        {
            return GenericResult<INotificationResult>.Failure(
                NotificationLogger.EmailSendFailed(_logger, ex, ex.Message));
        }
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

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return string.Equals(addr.Address, email, StringComparison.Ordinal);
        }
        catch (FormatException ex)
        {
            // Why: MailAddress throws FormatException for invalid email syntax — treat as invalid.
            // ex is observed (referenced below) so the exception is not silently discarded.
            _ = ex;
            return false;
        }
        catch (ArgumentException ex)
        {
            // Why: MailAddress throws ArgumentException for malformed display-name portions.
            _ = ex;
            return false;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // No unmanaged resources to dispose
    }
}
