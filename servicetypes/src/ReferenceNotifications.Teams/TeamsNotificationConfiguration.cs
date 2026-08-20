using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Abstractions;
using Fdw.Services.Notifications;

namespace ReferenceNotifications.Teams;

/// <summary>Configuration for Microsoft Teams notifications.</summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Notification", ServiceType = "Teams")]
public sealed partial class TeamsNotificationConfiguration : NotificationConfiguration
{
    /// <summary>Initializes a new instance of the <see cref="TeamsNotificationConfiguration"/> class.</summary>
    public TeamsNotificationConfiguration() : base("Notification", "Teams", "Notifications:Teams")
    {
    }

    // ========================================
    // Runtime fields (not on parent header)
    // Why: Polymorphic configuration pattern — parent is identity-only.
    // These fields are specific to the typed body and read by the factory at service construction time.
    // ========================================

    /// <summary>Gets or sets the service lifetime this notification service is registered with.</summary>
    public IServiceLifetime Lifetime { get; set; } = ServiceLifetimes.Transient;

    /// <summary>Gets or sets a value indicating whether this notification service is enabled.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Gets or sets the secret manager that resolves this channel's webhook URL.</summary>
    public string? SecretManagerName { get; set; }

    /// <summary>Gets or sets the key the secret manager resolves the webhook URL from.</summary>
    public string? SecretKeyName { get; set; }

    // ========================================
    // Teams-specific fields
    // ========================================

    /// <summary>Gets or sets the webhook posted to when a request names no recipient of its own.</summary>
    public string? DefaultWebhookUrl { get; set; }

    /// <summary>Gets or sets how long a post to Teams may take before it is abandoned.</summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets a value indicating whether messages are sent as Adaptive Cards rather than
    /// the legacy MessageCard payload.
    /// </summary>
    public bool UseAdaptiveCards { get; set; } = true;
}
