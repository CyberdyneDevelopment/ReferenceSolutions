using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Abstractions;
using Fdw.Services.Notifications;
using Fdw.Services.Notifications.Abstractions.Configuration;

namespace ReferenceNotifications.Email;

/// <summary>
/// Configuration for email notification services.
/// </summary>
/// <remarks>
/// <para>
/// This configuration inherits common properties (Id, Name, ServiceOptionType, etc.)
/// from <see cref="NotificationConfiguration"/> and adds email-specific settings
/// like SMTP host, port, authentication, and sender details.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Notification",
    ServiceType = "Email")]
public sealed partial class EmailNotificationConfiguration : NotificationConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EmailNotificationConfiguration"/> class.
    /// </summary>
    public EmailNotificationConfiguration() : base("Notification", "Email", "Notifications:Email")
    {
    }

    // ========================================
    // Runtime fields (not on parent header)
    // Why: Polymorphic configuration pattern — parent is identity-only.
    // These fields are specific to the typed body and read by the factory at service construction time.
    // ========================================

    /// <summary>
    /// Gets or sets the service lifetime for DI registration.
    /// </summary>
    public IServiceLifetime Lifetime { get; set; } = ServiceLifetimes.Transient;

    /// <summary>
    /// Gets or sets whether this notification channel is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the name of the secret manager to use for retrieving secrets.
    /// </summary>
    public string? SecretManagerName { get; set; }

    /// <summary>
    /// Gets or sets the key name within the secret manager to retrieve.
    /// </summary>
    public string? SecretKeyName { get; set; }

    // ========================================
    // Email-specific fields
    // ========================================

    /// <summary>
    /// Gets or sets the SMTP server host.
    /// </summary>
    public string SmtpHost { get; set; } = "localhost";

    /// <summary>
    /// Gets or sets the SMTP server port.
    /// </summary>
    public int SmtpPort { get; set; } = 25;

    /// <summary>
    /// Gets or sets whether to use SSL/TLS.
    /// </summary>
    public bool UseSsl { get; set; } = true;

    /// <summary>
    /// Gets or sets the SMTP authentication username.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the SMTP authentication password.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the sender email address.
    /// </summary>
    public string FromAddress { get; set; } = "noreply@example.com";

    /// <summary>
    /// Gets or sets the sender display name.
    /// </summary>
    public string? FromName { get; set; }

    /// <summary>
    /// Gets or sets the reply-to email address.
    /// </summary>
    public string? ReplyToAddress { get; set; }
}
