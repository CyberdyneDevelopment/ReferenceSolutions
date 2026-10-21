using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Abstractions;
using Fdw.Services.Notifications.Abstractions;

namespace FdwReferenceNotifications.Webhook;

/// <summary>
/// Configuration for webhook notification services.
/// </summary>
/// <remarks>
/// <para>
/// This configuration inherits common properties (Id, Name, Implementation, etc.)
/// from <c>NotificationConfiguration</c> and adds webhook-specific settings
/// like URL, HTTP method, headers, content type, payload template, retry count, and timeout.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Notification",
    ServiceType = "Webhook")]
public sealed partial class WebhookNotificationConfiguration : INotificationImplementationConfiguration
{
    /// <inheritdoc/>
    public Guid Id { get; set; }

    /// <inheritdoc/>
    // Why it maps with no column of its own: the name is the domain's, and there is one
    // name for a configured member. The domain provider joins the domain row to this one,
    // and the join's result set is what carries it in.
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the domain this implementation belongs to.</summary>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;

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
    /// Gets or sets how many more times a delivery is tried when the receiver cannot be reached, does not answer in time or answers with a
    /// server error (FDW-1913); a refusal is not tried again. Must not be negative. Defaults to 3.
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Gets or sets the request timeout in seconds. Defaults to 30.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}
