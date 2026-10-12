using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Abstractions;
using Fdw.Services.Notifications.Abstractions;

namespace FdwReferenceNotifications.Teams;

/// <summary>Configuration for Microsoft Teams notifications.</summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Notification", ServiceType = "Teams")]
public sealed partial class TeamsNotificationConfiguration : INotificationImplementationConfiguration
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

    /// <summary>Initializes a new instance of the <see cref="TeamsNotificationConfiguration"/> class.</summary>

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
