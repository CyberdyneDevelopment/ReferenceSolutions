using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Abstractions;
using Fdw.Services.Notifications;
using Fdw.Services.Notifications.Abstractions.Configuration;

namespace ReferenceNotifications.Webhook;

/// <summary>
/// Configuration for webhook notification services.
/// </summary>
/// <remarks>
/// <para>
/// This configuration inherits common properties (Id, Name, ServiceOptionType, etc.)
/// from <see cref="NotificationConfiguration"/> and adds webhook-specific settings
/// like URL, HTTP method, headers, content type, payload template, retry count, and timeout.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Notification",
    ServiceType = "Webhook")]
public sealed partial class WebhookNotificationConfiguration : NotificationConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookNotificationConfiguration"/> class.
    /// </summary>
    public WebhookNotificationConfiguration() : base("Notification", "Webhook", "Notifications:Webhook")
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
    // Webhook-specific fields
    // ========================================

    /// <summary>
    /// Gets or sets the webhook URL to POST notifications to.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets the HTTP method to use (POST, PUT). Defaults to POST.
    /// </summary>
    public string Method { get; set; } = "POST";

    /// <summary>
    /// Gets or sets the content type for the request body. Defaults to application/json.
    /// </summary>
    public string ContentType { get; set; } = "application/json";

    /// <summary>
    /// Gets or sets an optional custom payload template. When set, {subject}, {body},
    /// {type}, {timestamp} tokens are replaced. When null, a standard JSON payload is used.
    /// </summary>
    public string? PayloadTemplate { get; set; }

    /// <summary>
    /// Gets or sets the number of retry attempts on transient failures. Defaults to 3.
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Gets or sets the request timeout in seconds. Defaults to 30.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}
